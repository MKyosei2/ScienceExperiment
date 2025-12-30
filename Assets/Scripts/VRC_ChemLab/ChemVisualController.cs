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

    private void Start()
    {
        if (solidObj == null) solidObj = FindChild("Solid");
        if (liquidObj == null) liquidObj = FindChild("Liquid");
        if (gasObj == null) gasObj = FindChild("Gas");

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    // called by spawner (async)
    public void NotifyElementSelected(string symbol)
    {
        lastSelectedSymbol = symbol == null ? "" : symbol;
        // dropAnimatorがあれば「投入っぽい動き」を開始
        if (dropAnimator != null)
        {
            dropAnimator.SendCustomEvent("_PlayDrop");
        }
    }

    public void ApplyElementBySymbol(ChemElementDatabase db, string symbolOrFormula, float temperatureC)
    {
        if (db == null) return;

        // element symbol を優先（"NaCl"等は先頭2文字/1文字から拾う簡易）
        string sym = ExtractSymbol(db, symbolOrFormula);

        Color c = db.GetColor(sym);
        lastSelectedColor = c;

        // 状態判定（MP/BP）
        float mp = db.GetMP(sym);
        float bp = db.GetBP(sym);

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

        if (targetRenderers == null) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
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
