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
/// 2026-01:
/// - 既知化合物テーブル（ChemElementDatabase内）を参照して本格表示
/// - 未知化合物は式から組成を推定し、Particle/Shader用の"レシピ"を生成（ローカル）
/// </summary>
public class ChemVisualController : UdonSharpBehaviour
{
    // -----------------------------
    // Archetype / Particle Preset (int codes)
    // -----------------------------
    public const int ARCH_CRYSTAL = 0;
    public const int ARCH_POWDER = 1;
    public const int ARCH_METAL = 2;
    public const int ARCH_LIQUID = 3;
    public const int ARCH_GASFOG = 4;

    public const int PT_NONE = 0;
    public const int PT_GLINT = 1;
    public const int PT_PRECIPITATE = 2;
    public const int PT_BUBBLE = 3;
    public const int PT_FOG = 4;

    [Header("State Visuals (optional)")]
    public GameObject solidObj;
    public GameObject liquidObj;
    public GameObject gasObj;

    [Header("Renderer Targets (optional)")]
    public Renderer[] targetRenderers;

    [Header("Particles (optional)")]
    [Tooltip("ParticleSystemも元素カラーに追従させます(ParticleSystem.main.startColor を更新)。")]
    public bool applyColorToParticles = true;

    [Header("Shader Property Names (optional)")]
    public string propBaseColor = "_BaseColor";
    public string propColorFallback = "_Color";
    public string propOpacity = "_Opacity";
    public string propMetallic = "_Metallic";
    public string propSmoothness = "_Smoothness";
    public string propGlossinessFallback = "_Glossiness";
    public string propEmissionStrength = "_EmissionStrength";
    public string propEmissionColor = "_EmissionColor";
    public string propNoiseScale = "_NoiseScale";
    public string propDissolve = "_Dissolve";

    [Header("Product Token (optional, async)")]
    [Tooltip("生成物を器具内に残すためのトークン（任意）。完了時のみ表示。")]
    public GameObject productTokenObj;

    [Tooltip("トークンの見た目を変えるRenderer群（未指定ならproductTokenObj配下を自動収集）。")]
    public Renderer[] productTokenRenderers;

    [Tooltip("トークンを置く位置（任意）。未指定なら dropEnd、さらに無ければ自身。")]
    public Transform productTokenAnchor;

    public bool showProductTokenOnComplete = true;

    [Header("Optional Drop Animation (async)")]
    public UdonSharpBehaviour dropAnimator; // 追加スクリプトがある場合だけ使用
    public Transform dropStart;
    public Transform dropEnd;

    // last info (for other scripts)
    [HideInInspector] public string lastSelectedSymbol; // dropAnimatorが参照
    [HideInInspector] public Color lastSelectedColor;   // dropAnimatorが参照

    [Header("Last Recipe (read-only, for UI/VFX)")]
    [HideInInspector] public bool lastIsElement;
    [HideInInspector] public bool lastIsKnownCompound;
    [HideInInspector] public string lastRecipeSource;     // "element" / "known" / "inferred"
    [HideInInspector] public string lastInferenceNote;    // short note for UI
    [HideInInspector] public int lastArchetype;
    [HideInInspector] public int lastParticlePreset;

    [HideInInspector] public float lastOpacity;
    [HideInInspector] public float lastMetallic;
    [HideInInspector] public float lastSmoothness;
    [HideInInspector] public float lastEmission;
    [HideInInspector] public float lastNoiseScale;
    [HideInInspector] public float lastFogDensity;
    [HideInInspector] public float lastBubbleRate;
    [HideInInspector] public float lastViscosity;
    [HideInInspector] public float lastDensity;

    // cache (avoid material touching / SetActive spam)
    private bool _hasLastState;
    private ChemElementState _lastState;

    private bool _tokenActive;

    private bool _hasLastRecipe;
    private Color _lastBaseColor;
    private float _lastOpacity;
    private float _lastMetallic;
    private float _lastSmoothness;
    private float _lastEmission;
    private float _lastNoiseScale;

    // Particles under this visual (optional)
    private ParticleSystem[] _particleSystems;

    // temp buffers for formula parsing
    private const int MAX_ELEMS = 12;
    private string[] _tmpSyms;
    private int[] _tmpCounts;

    // When a prefab is cloned at runtime and immediately configured, Unity/Udon may call
    // our methods before Start() runs. We therefore expose an explicit initializer.
    private bool _initialized;

    private void Start()
    {
        EnsureInitialized();

        // IMPORTANT: The project keeps a scene-fixed SampleVisual under
        // ExperimentTable/VR_StartZone/ElementEffectAnchor/SampleVisual as a TEMPLATE.
        // That template must NOT emit particles or render, otherwise the world gets
        // flooded ("overflow"). Runtime clones will be enabled by the spawner.
        if (IsSceneTemplateSampleVisual())
        {
            MuteAsSceneTemplate();
        }
    }

    /// <summary>
    /// Safe initialization entry-point.
    /// Call this before ApplyElementBySymbol() when the object was spawned/cloned at runtime.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        if (solidObj == null) solidObj = FindChild("Solid");
        if (liquidObj == null) liquidObj = FindChild("Liquid");
        if (gasObj == null) gasObj = FindChild("Gas");

        // Collect all renderers under this visual (including inactive children).
        // NOTE: Some prefabs assign targetRenderers manually and may forget ParticleSystemRenderer,
        // which results in particles staying a constant color.
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = childRenderers;
        }
        else
        {
            targetRenderers = MergeUniqueRenderers(targetRenderers, childRenderers);
        }

        // Cache particle systems (optional)
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

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
                productTokenRenderers = productTokenObj.GetComponentsInChildren<Renderer>();
            }
            productTokenObj.SetActive(false);
            _tokenActive = false;
        }

        _tmpSyms = new string[MAX_ELEMS];
        _tmpCounts = new int[MAX_ELEMS];
    }

    private Renderer[] MergeUniqueRenderers(Renderer[] a, Renderer[] b)
    {
        if (a == null || a.Length == 0) return b;
        if (b == null || b.Length == 0) return a;

        int count = a.Length;
        for (int i = 0; i < b.Length; i++)
        {
            Renderer r = b[i];
            if (r == null) continue;
            bool exists = false;
            for (int j = 0; j < a.Length; j++)
            {
                if (a[j] == r)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists) count++;
        }

        if (count == a.Length) return a;

        Renderer[] merged = new Renderer[count];
        for (int i = 0; i < a.Length; i++) merged[i] = a[i];

        int idx = a.Length;
        for (int i = 0; i < b.Length; i++)
        {
            Renderer r = b[i];
            if (r == null) continue;
            bool exists = false;
            for (int j = 0; j < a.Length; j++)
            {
                if (a[j] == r)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                merged[idx] = r;
                idx++;
            }
        }
        return merged;
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

            // product token should match current recipe (color/material)
            ApplyLastRecipeToToken();
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

    /// <summary>
    /// Main visual API called by spawner.
    /// - Uses element DB for element symbols
    /// - Uses known compound tables when available
    /// - Otherwise: infer a VisualRecipe from the formula (local, deterministic)
    /// </summary>
    public void ApplyElementBySymbol(ChemElementDatabase db, string symbolOrFormula, float temperatureC)
    {
        if (db == null) return;

        string input = symbolOrFormula == null ? "" : symbolOrFormula.Trim();
        if (string.IsNullOrEmpty(input))
        {
            lastSelectedColor = Color.white;
            lastInferenceNote = "";
            lastRecipeSource = "none";
            lastIsElement = false;
            lastIsKnownCompound = false;
            lastArchetype = ARCH_CRYSTAL;
            lastParticlePreset = PT_NONE;
            SetState(ChemElementState.Solid);
            ApplyRecipe(Color.white, 1f, 0f, 0.4f, 0f, 0.2f, 0f);
            return;
        }

        // Element symbol を優先（"NaCl"等は先頭2文字/1文字から拾う簡易）
        string sym = ExtractSymbol(db, input);
        bool isElement = db.ContainsSymbol(sym);

        // Outputs
        Color baseColor;
        float mp;
        float bp;
        int hazard;
        int archetype;
        int particlePreset;
        float opacity;
        float metallic;
        float smoothness;
        float emission;
        float noiseScale;
        float fogDensity;
        float bubbleRate;
        float viscosity;
        float density;

        bool knownCompound = false;
        string recipeSource = "";
        string inference = "";

        if (isElement)
        {
            recipeSource = "element";
            baseColor = db.GetColor(sym);
            mp = db.GetMP(sym);
            bp = db.GetBP(sym);
            hazard = db.GetHazard(sym);

            // safety: if not defined
            if (float.IsNaN(mp) || float.IsInfinity(mp)) mp = 25f;
            if (float.IsNaN(bp) || float.IsInfinity(bp)) bp = 100f;

            float metal01 = db.GetIsMetal(sym) ? 1f : 0f;
            archetype = db.GetIsMetal(sym) ? ARCH_METAL : ARCH_CRYSTAL;
            particlePreset = db.GetIsMetal(sym) ? PT_GLINT : PT_NONE;

            opacity = 0.98f;
            metallic = metal01;
            smoothness = db.GetIsMetal(sym) ? 0.65f : 0.40f;
            emission = (hazard & ChemElementDatabase.HAZ_RADIOACTIVE) != 0 ? 0.20f : 0.03f;
            noiseScale = 0.25f;
            fogDensity = 0.60f;
            bubbleRate = 0f;
            viscosity = 1f;
            density = 1f;

            inference = db.GetNameJa(sym);
        }
        else
        {
            // try known compound table first
            knownCompound = db.TryGetCompoundRecipe(input,
                out baseColor,
                out mp,
                out bp,
                out hazard,
                out archetype,
                out particlePreset,
                out opacity,
                out metallic,
                out smoothness,
                out emission,
                out noiseScale,
                out fogDensity,
                out bubbleRate,
                out viscosity,
                out density);

            if (knownCompound)
            {
                recipeSource = "known";
                inference = db.GetCompoundNameJa(input);

                if (float.IsNaN(mp) || float.IsInfinity(mp)) mp = 25f;
                if (float.IsNaN(bp) || float.IsInfinity(bp)) bp = 100f;

                // If table has no archetype, decide from mp/bp
                if (archetype < 0) archetype = ARCH_CRYSTAL;
            }
            else
            {
                recipeSource = "inferred";
                InferCompoundRecipe(db, input, temperatureC,
                    out baseColor,
                    out mp,
                    out bp,
                    out hazard,
                    out archetype,
                    out particlePreset,
                    out opacity,
                    out metallic,
                    out smoothness,
                    out emission,
                    out noiseScale,
                    out fogDensity,
                    out bubbleRate,
                    out viscosity,
                    out density,
                    out inference);
            }
        }

        lastSelectedSymbol = input;
        lastSelectedColor = baseColor;

        lastIsElement = isElement;
        lastIsKnownCompound = knownCompound;
        lastRecipeSource = recipeSource;
        lastInferenceNote = inference;
        lastArchetype = archetype;
        lastParticlePreset = particlePreset;

        lastOpacity = opacity;
        lastMetallic = metallic;
        lastSmoothness = smoothness;
        lastEmission = emission;
        lastNoiseScale = noiseScale;
        lastFogDensity = fogDensity;
        lastBubbleRate = bubbleRate;
        lastViscosity = viscosity;
        lastDensity = density;

        // determine state from mp/bp
        ChemElementState state;
        if (temperatureC < mp) state = ChemElementState.Solid;
        else if (temperatureC < bp) state = ChemElementState.Liquid;
        else state = ChemElementState.Gas;

        // If archetype indicates gas/liquid strongly, override (keeps visuals consistent)
        if (archetype == ARCH_GASFOG) state = ChemElementState.Gas;
        else if (archetype == ARCH_LIQUID) state = ChemElementState.Liquid;

        SetState(state);

        // dissolve can be used for transitions (optional). Here keep 0.
        ApplyRecipe(baseColor, opacity, metallic, smoothness, emission, noiseScale, 0f);

        // token should match while active
        ApplyLastRecipeToToken();
    }

    private void ApplyLastRecipeToToken()
    {
        if (!_tokenActive) return;
        if (productTokenRenderers == null || productTokenRenderers.Length == 0) return;

        ApplyRecipeToRenderers(productTokenRenderers,
            lastSelectedColor,
            lastOpacity,
            lastMetallic,
            lastSmoothness,
            lastEmission,
            lastNoiseScale,
            0f);
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

    /// <summary>
    /// Apply shader recipe to main renderers.
    /// </summary>
    private void ApplyRecipe(Color baseColor, float opacity, float metallic, float smoothness, float emission, float noiseScale, float dissolve)
    {
        // Cache check
        if (_hasLastRecipe)
        {
            float dr = baseColor.r - _lastBaseColor.r;
            float dg = baseColor.g - _lastBaseColor.g;
            float db = baseColor.b - _lastBaseColor.b;
            float da = opacity - _lastOpacity;
            float dm = metallic - _lastMetallic;
            float ds = smoothness - _lastSmoothness;
            float de = emission - _lastEmission;
            float dn = noiseScale - _lastNoiseScale;

            if ((dr * dr + dg * dg + db * db + da * da + dm * dm + ds * ds + de * de + dn * dn) < 0.000001f)
            {
                return;
            }
        }

        _hasLastRecipe = true;
        _lastBaseColor = baseColor;
        _lastOpacity = opacity;
        _lastMetallic = metallic;
        _lastSmoothness = smoothness;
        _lastEmission = emission;
        _lastNoiseScale = noiseScale;

        ApplyRecipeToRenderers(targetRenderers, baseColor, opacity, metallic, smoothness, emission, noiseScale, dissolve);

        // Ensure particle start color follows the element (fix: particles staying constant color)
        if (applyColorToParticles) ApplyColorToParticleSystems(baseColor, opacity);
    }

    private void ApplyColorToParticleSystems(Color baseColor, float opacity)
    {
        if (_particleSystems == null || _particleSystems.Length == 0) return;

        Color c = baseColor;
        c.a = Mathf.Clamp01(opacity);

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem ps = _particleSystems[i];
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = c;
        }
    }

    private void ApplyRecipeToRenderers(Renderer[] renderers, Color baseColor, float opacity, float metallic, float smoothness, float emission, float noiseScale, float dissolve)
    {
        if (renderers == null) return;

        // ---- Robustness for Udon/CSV data ----
        // If any parameter becomes NaN/Infinity (e.g. missing data), materials may render invisible.
        // Sanitize values to guaranteed-visible defaults.
        if (float.IsNaN(opacity) || float.IsInfinity(opacity)) opacity = 1f;
        if (float.IsNaN(metallic) || float.IsInfinity(metallic)) metallic = 0f;
        if (float.IsNaN(smoothness) || float.IsInfinity(smoothness)) smoothness = 0.4f;
        if (float.IsNaN(emission) || float.IsInfinity(emission)) emission = 0f;
        if (float.IsNaN(noiseScale) || float.IsInfinity(noiseScale)) noiseScale = 0.25f;
        if (float.IsNaN(dissolve) || float.IsInfinity(dissolve)) dissolve = 0f;

        if (float.IsNaN(baseColor.r) || float.IsInfinity(baseColor.r) ||
            float.IsNaN(baseColor.g) || float.IsInfinity(baseColor.g) ||
            float.IsNaN(baseColor.b) || float.IsInfinity(baseColor.b) ||
            float.IsNaN(baseColor.a) || float.IsInfinity(baseColor.a))
        {
            baseColor = Color.white;
        }

        Color c = baseColor;
        c.a = Mathf.Clamp01(opacity);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            Material m = r.material;
            if (m == null) continue;

            // base color
            if (!string.IsNullOrEmpty(propBaseColor) && m.HasProperty(propBaseColor)) m.SetColor(propBaseColor, c);
            if (!string.IsNullOrEmpty(propColorFallback) && m.HasProperty(propColorFallback)) m.SetColor(propColorFallback, c);

            // opacity (if custom shader supports)
            if (!string.IsNullOrEmpty(propOpacity) && m.HasProperty(propOpacity)) m.SetFloat(propOpacity, opacity);

            // metallic / smoothness
            if (!string.IsNullOrEmpty(propMetallic) && m.HasProperty(propMetallic)) m.SetFloat(propMetallic, metallic);

            if (!string.IsNullOrEmpty(propSmoothness) && m.HasProperty(propSmoothness))
                m.SetFloat(propSmoothness, smoothness);
            else if (!string.IsNullOrEmpty(propGlossinessFallback) && m.HasProperty(propGlossinessFallback))
                m.SetFloat(propGlossinessFallback, smoothness);

            // emission
            if (!string.IsNullOrEmpty(propEmissionStrength) && m.HasProperty(propEmissionStrength))
                m.SetFloat(propEmissionStrength, emission);
            if (!string.IsNullOrEmpty(propEmissionColor) && m.HasProperty(propEmissionColor))
                m.SetColor(propEmissionColor, Color.white * emission);

            // noise / dissolve
            if (!string.IsNullOrEmpty(propNoiseScale) && m.HasProperty(propNoiseScale))
                m.SetFloat(propNoiseScale, noiseScale);
            if (!string.IsNullOrEmpty(propDissolve) && m.HasProperty(propDissolve))
                m.SetFloat(propDissolve, dissolve);
        }
    }

    // ==========================================
    // Inference (未知化合物)
    // ==========================================

    private void InferCompoundRecipe(
        ChemElementDatabase db,
        string formula,
        float temperatureC,
        out Color baseColor,
        out float mpC,
        out float bpC,
        out int hazard,
        out int archetype,
        out int particlePreset,
        out float opacity,
        out float metallic,
        out float smoothness,
        out float emission,
        out float noiseScale,
        out float fogDensity,
        out float bubbleRate,
        out float viscosity,
        out float density,
        out string note
    )
    {
        if (formula == null) formula = "";
        hazard = db.GetHazardForFormulaOrElement(formula);

        int n = ParseFormulaToBuffers(db, formula);
        int total = 0;
        int metalCount = 0;

        float enMin = 999f;
        float enMax = 0f;
        bool hasEN = false;

        float massSum = 0f;
        Color colSum = new Color(0f, 0f, 0f, 0f);

        for (int i = 0; i < n; i++)
        {
            string s = _tmpSyms[i];
            int c = _tmpCounts[i];
            if (c <= 0) continue;

            total += c;

            Color ec = db.GetColor(s);
            colSum.r += ec.r * c;
            colSum.g += ec.g * c;
            colSum.b += ec.b * c;

            float am = db.GetAtomicMass(s);
            if (am > 0f) massSum += am * c;

            float en = db.GetElectronegativity(s);
            if (en > 0f)
            {
                hasEN = true;
                if (en < enMin) enMin = en;
                if (en > enMax) enMax = en;
            }

            if (db.GetIsMetal(s)) metalCount += c;
        }

        if (total <= 0)
        {
            // fallback deterministic
            int h = 17;
            for (int i = 0; i < formula.Length; i++) h = (h * 31) + (int)formula[i];
            float hue = Mathf.Repeat((h & 0x7fffffff) * 0.0001f, 1f);
            baseColor = Color.HSVToRGB(hue, 0.35f, 1f);
            mpC = 25f;
            bpC = 100f;
            archetype = ARCH_CRYSTAL;
            particlePreset = PT_NONE;
            opacity = 0.95f;
            metallic = 0f;
            smoothness = 0.35f;
            emission = 0.03f;
            noiseScale = 0.35f;
            fogDensity = 0.7f;
            bubbleRate = 0.2f;
            viscosity = 1f;
            density = 1f;
            note = "推定：組成解析失敗";
            return;
        }

        baseColor = new Color(colSum.r / total, colSum.g / total, colSum.b / total, 1f);

        float metalRatio = (float)metalCount / (float)total;
        float deltaEN = hasEN ? (enMax - enMin) : 0f;
        float ionic01 = hasEN ? Mathf.Clamp01((deltaEN - 0.8f) / 1.5f) : 0.25f;
        float avgMass = massSum > 0f ? (massSum / total) : 30f;

        // Color adjustments: ionic -> desaturate toward white, metal -> toward gray
        if (ionic01 > 0.05f)
        {
            baseColor = Color.Lerp(baseColor, Color.white, ionic01 * 0.55f);
        }
        if (metalRatio > 0.05f)
        {
            float lum = (baseColor.r + baseColor.g + baseColor.b) / 3f;
            Color gray = new Color(lum, lum, lum, 1f);
            baseColor = Color.Lerp(baseColor, gray, metalRatio * 0.65f);
        }

        // Estimate mp/bp for phase visualization (educational, not scientific)
        mpC = 10f + ionic01 * 420f + metalRatio * 260f + Mathf.Clamp(avgMass, 5f, 150f) * 0.8f;
        bpC = mpC + 90f + (1f - ionic01) * 120f + Mathf.Clamp(avgMass, 5f, 250f) * 0.6f;

        // Decide archetype mainly by solid composition; state will override when hot
        if (temperatureC >= bpC)
        {
            archetype = ARCH_GASFOG;
            particlePreset = PT_FOG;
        }
        else if (temperatureC >= mpC)
        {
            archetype = ARCH_LIQUID;
            particlePreset = PT_BUBBLE;
        }
        else
        {
            if (metalRatio >= 0.65f)
            {
                archetype = ARCH_METAL;
                particlePreset = PT_GLINT;
            }
            else if (ionic01 >= 0.60f)
            {
                archetype = ARCH_CRYSTAL;
                particlePreset = PT_GLINT;
            }
            else
            {
                archetype = ARCH_POWDER;
                particlePreset = PT_NONE;
            }
        }

        // Visual parameters
        if (archetype == ARCH_GASFOG)
        {
            opacity = 0.07f;
            metallic = 0f;
            smoothness = 0f;
            emission = 0.01f;
            noiseScale = 0.45f;
            fogDensity = 0.65f + 0.35f * Mathf.Clamp01(avgMass / 120f);
            bubbleRate = 0f;
            viscosity = 0f;
            density = 0.6f + 0.6f * Mathf.Clamp01(avgMass / 120f);
        }
        else if (archetype == ARCH_LIQUID)
        {
            opacity = 0.12f;
            metallic = Mathf.Clamp01(metalRatio * 0.2f);
            smoothness = 0.88f;
            emission = 0.02f;
            noiseScale = 0.22f;
            fogDensity = 0.0f;
            bubbleRate = 0.25f + 0.55f * (1f - ionic01);
            viscosity = 0.7f + 1.0f * Mathf.Clamp01(avgMass / 120f);
            density = 0.8f + 0.7f * Mathf.Clamp01(avgMass / 120f);
        }
        else
        {
            opacity = (archetype == ARCH_POWDER) ? 0.95f : 0.98f;
            metallic = Mathf.Clamp01(metalRatio);
            smoothness = (archetype == ARCH_METAL) ? 0.70f : 0.40f;
            emission = (hazard & ChemElementDatabase.HAZ_RADIOACTIVE) != 0 ? 0.25f : 0.03f;
            noiseScale = (archetype == ARCH_POWDER) ? 0.55f : 0.28f;
            fogDensity = 0.0f;
            bubbleRate = 0.0f;
            viscosity = 0f;
            density = 1.0f;
        }

        // Compose short inference note
        string mode = (archetype == ARCH_GASFOG) ? "気体" : (archetype == ARCH_LIQUID) ? "液体" : (archetype == ARCH_METAL) ? "金属" : (archetype == ARCH_POWDER) ? "粉末" : "結晶";
        note = "推定:" + mode;
        if (hasEN) note += " ΔEN=" + deltaEN.ToString("0.0");
        note += " 金属率=" + Mathf.RoundToInt(metalRatio * 100f) + "%";
    }

    /// <summary>
    /// Parse formula into internal buffers.
    /// Supports: H2O, NaCl, CO2, NH3, CuSO4, dots (CuSO4.5H2O), leading coefficients (5H2O).
    /// No parentheses support.
    /// Returns unique element count stored in _tmpSyms/_tmpCounts.
    /// </summary>
    private int ParseFormulaToBuffers(ChemElementDatabase db, string formula)
    {
        for (int i = 0; i < MAX_ELEMS; i++)
        {
            _tmpSyms[i] = "";
            _tmpCounts[i] = 0;
        }

        if (string.IsNullOrEmpty(formula)) return 0;

        string f = formula;
        if (f == null) return 0;
        f = f.Trim();
        f = f.Replace(" ", "");
        f = f.Replace("·", ".");
        if (string.IsNullOrEmpty(f)) return 0;
        int len = f.Length;

        int unique = 0;
        int partMul = 1;
        bool atPartStart = true;

        int iPos = 0;
        while (iPos < len)
        {
            char ch = f[iPos];

            // separator (hydrate dot)
            if (ch == '.')
            {
                partMul = 1;
                atPartStart = true;
                iPos++;
                continue;
            }

            // skip non-ascii letters
            if (ch == '+' || ch == '-' || ch == '=' || ch == '(' || ch == ')' || ch == '[' || ch == ']' )
            {
                iPos++;
                continue;
            }

            // coefficient at part start: e.g. 5H2O
            if (atPartStart && ch >= '0' && ch <= '9')
            {
                int num = 0;
                while (iPos < len)
                {
                    char d = f[iPos];
                    if (d < '0' || d > '9') break;
                    num = (num * 10) + (d - '0');
                    iPos++;
                }
                partMul = Mathf.Clamp(num, 1, 99);
                atPartStart = false;
                continue;
            }

            // element token
            if (ch >= 'A' && ch <= 'Z')
            {
                int start = iPos;
                iPos++;
                if (iPos < len)
                {
                    char lo = f[iPos];
                    if (lo >= 'a' && lo <= 'z') iPos++;
                }
                string raw = f.Substring(start, iPos - start);

                // validate with DB; if unknown and 2 letters, try 1 letter
                string sym = raw;
                if (!db.ContainsSymbol(sym) && sym.Length == 2)
                {
                    string s1 = sym.Substring(0, 1);
                    if (db.ContainsSymbol(s1)) sym = s1;
                }

                // count digits
                int num = 0;
                bool hasNum = false;
                while (iPos < len)
                {
                    char d = f[iPos];
                    if (d < '0' || d > '9') break;
                    hasNum = true;
                    num = (num * 10) + (d - '0');
                    iPos++;
                }
                int count = hasNum ? num : 1;
                count *= partMul;

                // add / accumulate
                int found = -1;
                for (int k = 0; k < unique; k++)
                {
                    if (_tmpSyms[k] == sym)
                    {
                        found = k;
                        break;
                    }
                }
                if (found >= 0)
                {
                    _tmpCounts[found] += count;
                }
                else if (unique < MAX_ELEMS)
                {
                    _tmpSyms[unique] = sym;
                    _tmpCounts[unique] = count;
                    unique++;
                }

                atPartStart = false;
                continue;
            }

            // other characters
            atPartStart = false;
            iPos++;
        }

        return unique;
    }

    private GameObject FindChild(string childName)
    {
        Transform t = transform.Find(childName);
        return t != null ? t.gameObject : null;
    }

    // -----------------------------
    // Scene template protection
    // -----------------------------
    private bool IsSceneTemplateSampleVisual()
    {
        // This method exists ONLY to mute the single scene-template object at:
        //   ExperimentTable/VR_StartZone/ElementEffectAnchor/SampleVisual
        // We must NOT accidentally mute runtime-spawned visuals, otherwise effects disappear.

        if (gameObject == null) return false;
        if (gameObject.name != "SampleVisual") return false;

        bool underElementEffectAnchor = false;
        bool underStartZoneOrTable = false;

        Transform p = transform;
        int guard = 0;
        while (p != null && guard < 64)
        {
            string n = p.name;
            if (n == "ElementEffectAnchor") underElementEffectAnchor = true;
            if (n == "VR_StartZone" || n == "ExperimentTable") underStartZoneOrTable = true;

            // Early out once we have enough evidence
            if (underElementEffectAnchor && underStartZoneOrTable) return true;

            p = p.parent;
            guard++;
        }

        // If it's under an ElementEffectAnchor but NOT under the table/start zone,
        // it's almost certainly a runtime clone and must stay active.
        return false;
    }

    private void MuteAsSceneTemplate()
    {
        // Stop any particles + hide renderers on the template. Runtime clones are enabled by the spawner.
        ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i] == null) continue;
                var em = ps[i].emission;
                em.enabled = false;
                ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        if (rs != null)
        {
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                rs[i].enabled = false;
            }
        }
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
