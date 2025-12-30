using UdonSharp;
using UnityEngine;

/// <summary>
/// UdonSharp does not support nested type declarations.
/// Keep enums as top-level types.
/// </summary>
public enum ChemElementState
{
    Solid = 0,
    Liquid = 1,
    Gas = 2
}

/// <summary>
/// ChemVisualController (Async-only visuals)
/// ・同期の真実はChemElementSpawnerが持つ
/// ・ここは「どう見せるか」だけ（非同期）
///
/// 追加演出（元素が器具に入る見せ方）は、
/// optionalの dropAnimator に対して CustomEvent を送る方式で接続します。
/// </summary>
public class ChemVisualController : UdonSharpBehaviour
{
    [Header("State Visuals (optional)")]
    public GameObject solidObj;
    public GameObject liquidObj;
    public GameObject gasObj;

    [Header("Renderer Targets (optional)")]
    public Renderer[] targetRenderers;

    [Header("Product Token (optional, async)")]
    [Tooltip("生成物を器具内に残すためのトークン（任意）。完了時のみ表示。")]
    public GameObject productTokenObj;

    [Tooltip("トークンの色を変えるRenderer群（未指定ならproductTokenObj配下を自動収集）。")]
    public Renderer[] productTokenRenderers;

    [Tooltip("トークンを置く位置（任意）。未指定なら dropEnd、さらに無ければ自身。")]
    public Transform productTokenAnchor;

    public bool showProductTokenOnComplete = true;

    [Header("Optional Drop Animation (async)")]
    public UdonSharpBehaviour dropAnimator; // 追加スクリプトがある場合だけ使用
    public Transform dropStart;
    public Transform dropEnd;

    [HideInInspector] public string lastSelectedSymbol; // dropAnimatorが参照
    [HideInInspector] public Color lastSelectedColor;   // dropAnimatorが参照

    // cache (avoid material touching / SetActive spam)
    private bool _hasLastState;
    private ChemElementState _lastState;
    private bool _hasLastColor;
    private Color _lastColor;
    private bool _tokenActive;

    private void Start()
    {
        if (solidObj == null) solidObj = FindChild("Solid");
        if (liquidObj == null) liquidObj = FindChild("Liquid");
        if (gasObj == null) gasObj = FindChild("Gas");

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        // ProductToken auto find
        if (productTokenObj == null)
        {
            GameObject pt = FindChild("ProductToken");
            if (pt != null) productTokenObj = pt;
        }

        if (productTokenObj != null)
        {
            if (productTokenRenderers == null || productTokenRenderers.Length == 0)
            {
                productTokenRenderers = productTokenObj.GetComponentsInChildren<Renderer>(true);
            }
            productTokenObj.SetActive(false);
            _tokenActive = false;
        }
    }

    // called by spawner (async)
    public void NotifyElementSelected(string symbol)
    {
        lastSelectedSymbol = symbol == null ? "" : symbol;

        // selecting a new input should hide previous product token
        NotifyExperimentReset();

        // dropAnimatorがあれば「投入っぽい動き」を開始
        if (dropAnimator != null)
        {
            dropAnimator.SendCustomEvent("_PlayDrop");
        }
    }

    // called by spawner (async) on reset/start
    public void NotifyExperimentReset()
    {
        SetProductTokenActive(false);
    }

    // called by spawner (async) when phase becomes complete
    public void NotifyReactionComplete(string productFormula, string reactionTag)
    {
        lastSelectedSymbol = productFormula == null ? "" : productFormula;

        // optional: pulse / drop to emphasize completion
        if (dropAnimator != null)
        {
            dropAnimator.SendCustomEvent("_PlayDrop");
        }

        if (showProductTokenOnComplete)
        {
            SetProductTokenActive(true);
            PlaceProductToken();
        }
    }

    private void SetProductTokenActive(bool on)
    {
        if (productTokenObj == null) return;
        if (_tokenActive == on) return;
        _tokenActive = on;
        productTokenObj.SetActive(on);
    }

    private void PlaceProductToken()
    {
        if (productTokenObj == null) return;
        Transform a = productTokenAnchor;
        if (a == null) a = dropEnd;
        if (a == null) a = transform;

        productTokenObj.transform.position = a.position;
        productTokenObj.transform.rotation = a.rotation;
    }

    public void ApplyElementBySymbol(ChemElementDatabase db, string symbolOrFormula, float temperatureC)
    {
        if (db == null) return;

        // element symbol を優先（"NaCl"等は先頭2文字/1文字から拾う簡易）
        string sym = ExtractSymbol(db, symbolOrFormula);

        bool isElement = db.ContainsSymbol(sym);

        Color c;
        float mp;
        float bp;

        if (isElement)
        {
            c = db.GetColor(sym);
            mp = db.GetMP(sym);
            bp = db.GetBP(sym);

            // safety: if not defined, fallback to room-temp thresholds
            if (float.IsNaN(mp)) mp = 25f;
            if (float.IsNaN(bp)) bp = 100f;
        }
        else
        {
            // compound fallback (for products like "NaCl", "H2O" etc.)
            GetCompoundFallback(symbolOrFormula, out c, out mp, out bp);
        }

        lastSelectedColor = c;

        ChemElementState state;
        if (temperatureC < mp) state = ChemElementState.Solid;
        else if (temperatureC < bp) state = ChemElementState.Liquid;
        else state = ChemElementState.Gas;

        SetState(state);
        ApplyColor(c);
    }

    public void SetState(ChemElementState s)
    {
        if (_hasLastState && _lastState == s) return;
        _hasLastState = true;
        _lastState = s;

        if (solidObj != null) solidObj.SetActive(s == ChemElementState.Solid);
        if (liquidObj != null) liquidObj.SetActive(s == ChemElementState.Liquid);
        if (gasObj != null) gasObj.SetActive(s == ChemElementState.Gas);
    }

    public void ApplyColor(Color c)
    {
        if (_hasLastColor)
        {
            float dr = c.r - _lastColor.r;
            float dg = c.g - _lastColor.g;
            float db = c.b - _lastColor.b;
            float da = c.a - _lastColor.a;
            if ((dr * dr + dg * dg + db * db + da * da) < 0.00001f)
            {
                return;
            }
        }

        _hasLastColor = true;
        _lastColor = c;

        ApplyColorToRenderers(targetRenderers, c);

        if (_tokenActive)
        {
            ApplyColorToRenderers(productTokenRenderers, c);
        }
    }

    private void ApplyColorToRenderers(Renderer[] renderers, Color c)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            Material m = r.material;
            if (m == null) continue;

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    private GameObject FindChild(string childName)
    {
        Transform t = transform.Find(childName);
        return t != null ? t.gameObject : null;
    }

    private void GetCompoundFallback(string formula, out Color color, out float mpC, out float bpC)
    {
        // Known common compounds (approx, for educational visualization)
        // Used only when formula is NOT an element symbol in the element database.
        if (string.IsNullOrEmpty(formula))
        {
            color = Color.white;
            mpC = 25f;
            bpC = 100f;
            return;
        }

        string f = formula.Trim();

        // Water
        if (f == "H2O" || f == "Water")
        {
            color = new Color(0.35f, 0.65f, 1f);
            mpC = 0f;
            bpC = 100f;
            return;
        }

        // Sodium chloride (table salt)
        if (f == "NaCl" || f == "Salt")
        {
            color = new Color(0.95f, 0.95f, 0.95f);
            mpC = 801f;
            bpC = 1413f;
            return;
        }

        // Generic fallback: visually distinct but deterministic per formula
        int h = 17;
        int len = f.Length;
        for (int i = 0; i < len; i++)
        {
            h = (h * 31) + (int)f[i];
        }

        float hue = Mathf.Repeat((h & 0x7fffffff) * 0.0001f, 1f);
        color = Color.HSVToRGB(hue, 0.35f, 1f);

        // Default thresholds (for state visualization only)
        mpC = 25f;
        bpC = 100f;
    }

    private string ExtractSymbol(ChemElementDatabase db, string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        string s = input.Trim();

        // 完全一致
        if (db.ContainsSymbol(s)) return s;

        // 先頭2文字/1文字を試す（H2O, NaCl のような簡易）
        if (s.Length >= 2)
        {
            string s2 = s.Substring(0, 2);
            if (db.ContainsSymbol(s2)) return s2;
        }
        string s1 = s.Substring(0, 1);
        if (db.ContainsSymbol(s1)) return s1;

        return s;
    }
}
