using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemReactionAnimator (Async VFX controller)
/// spawner / AIRequestSender が出した係数を、粒子・発光・液面などに適用する。
///
/// 2026-01:
/// - Particle/Shaderベースの"化合物っぽさ"を増やすため、
///   VisualControllerのlastParticlePresetを追加粒子へ反映できるようにした。
/// </summary>
public class ChemReactionAnimator : UdonSharpBehaviour
{
    [Header("Particles (optional)")]
    public ParticleSystem foamParticles;
    public ParticleSystem smokeParticles;
    public ParticleSystem sparkParticles;

    [Header("Compound Particles (optional)")]
    [Tooltip("Glint: 結晶/金属のきらめき")]
    public ParticleSystem glintParticles;
    [Tooltip("Precipitate: 析出")]
    public ParticleSystem precipitateParticles;
    [Tooltip("Bubble: 発泡")]
    public ParticleSystem bubbleParticles;
    [Tooltip("Fog: 気体/昇華の霧")]
    public ParticleSystem fogParticles;

    [Header("Particle Rates (optional)")]
    [Tooltip("Max emission rate for foam particles")]
    public float foamRateMax = 25f;
    [Tooltip("Max emission rate for smoke particles")]
    public float smokeRateMax = 20f;
    [Tooltip("Max emission rate for spark particles")]
    public float sparkRateMax = 90f;
    [Tooltip("Max emission rate for compound particles")]
    public float compoundRateMax = 18f;

    [Header("Glow (optional)")]
    public Renderer[] glowRenderers;
    public string emissionProperty = "_EmissionColor";
    [Range(0f, 10f)] public float emissionMax = 2.0f;

    [Header("Heat (optional)")]
    public Renderer[] heatRenderers;
    public string heatProperty = "_HeatStrength";
    [Range(0f, 5f)] public float heatMax = 1.0f;

    [Header("Wave (optional)")]
    public Renderer[] waveRenderers;
    public string waveProperty = "_WaveStrength";
    [Range(0f, 5f)] public float waveMax = 1.0f;

    private float _heat, _foam, _glow, _wave, _spark, _smoke;

    [Header("Auto Resolve (optional)")]
    [Tooltip("Inspector参照が未設定でも動くように、子階層からRenderers/Particlesを自動収集します。")]
    public bool autoResolveOnStart = true;

    private bool _initialized;
    private Material _localParticleMasterMaterial;

    // One-time constraint (prevents particles from flying outside the glassware)
    private bool _volumeConstrained;
    private Vector3 _cachedWCenter;
    private Vector3 _cachedWSize;

    private void Start()
    {
        if (autoResolveOnStart) EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        // ---- particles ----
        if (foamParticles == null) foamParticles = FindParticleByNameContains("Foam");
        if (smokeParticles == null) smokeParticles = FindParticleByNameContains("Smoke");
        if (sparkParticles == null) sparkParticles = FindParticleByNameContains("Spark");

        if (glintParticles == null) glintParticles = FindParticleByNameContains("Glint");
        if (precipitateParticles == null) precipitateParticles = FindParticleByNameContains("Precip");
        if (bubbleParticles == null) bubbleParticles = FindParticleByNameContains("Bubble");
        if (fogParticles == null) fogParticles = FindParticleByNameContains("Fog");

        // Ensure particle materials exist (some prefabs ship with material=None)
        EnsureParticleMaterial(foamParticles);
        EnsureParticleMaterial(smokeParticles);
        EnsureParticleMaterial(sparkParticles);
        EnsureParticleMaterial(glintParticles);
        EnsureParticleMaterial(precipitateParticles);
        EnsureParticleMaterial(bubbleParticles);
        EnsureParticleMaterial(fogParticles);

        // ---- renderers ----
        if (glowRenderers == null || glowRenderers.Length == 0)
            glowRenderers = FindRenderersWithProperty(emissionProperty);

        if (heatRenderers == null || heatRenderers.Length == 0)
            heatRenderers = FindRenderersWithProperty(heatProperty);

        if (waveRenderers == null || waveRenderers.Length == 0)
            waveRenderers = FindRenderersWithProperty(waveProperty);
    }

    // ---- public setters ----
    public void SetHeatLevel(float v)  { _heat = Mathf.Clamp01(v);  Apply(); }
    public void SetFoamLevel(float v)  { _foam = Mathf.Clamp01(v);  Apply(); }
    public void SetGlowLevel(float v)  { _glow = Mathf.Clamp01(v);  Apply(); }
    public void SetWaveLevel(float v)  { _wave = Mathf.Clamp01(v);  Apply(); }
    public void SetSparkLevel(float v) { _spark = Mathf.Clamp01(v); Apply(); }
    public void SetSmokeLevel(float v) { _smoke = Mathf.Clamp01(v); Apply(); }

    public void ResetLevels()
    {
        _heat = _foam = _glow = _wave = _spark = _smoke = 0f;
        Apply();

        // compound particles
        ApplyParticle(glintParticles, 0f, compoundRateMax);
        ApplyParticle(precipitateParticles, 0f, compoundRateMax);
        ApplyParticle(bubbleParticles, 0f, compoundRateMax);
        ApplyParticle(fogParticles, 0f, compoundRateMax);
    }

    /// <summary>
    /// Backward compatible: only AI based levels.
    /// </summary>
    public void ApplyPreset(string reactionTag, AIRequestSender ai, float progress01)
    {
        ApplyPreset(reactionTag, ai, progress01, null);
    }

    /// <summary>
    /// reactionTag と ai の係数からプリセット適用
    /// + VisualController の lastParticlePreset を追加粒子へ反映
    /// </summary>
    public void ApplyPreset(string reactionTag, AIRequestSender ai, float progress01, ChemVisualController visual)
    {
        EnsureInitialized();

        // Constrain particles to the current tool volume once (prevents particles leaving the glassware).
        // We do it lazily here because the animator is often cloned/parented at runtime.
        if (!_volumeConstrained)
        {
            ConstrainParticlesToContainerVolume(visual);
        }

        if (ai == null)
        {
            ResetLevels();
            return;
        }

        // 基本はai係数を使う。reactionTagが無い場合もこれで動く。
        _heat = Mathf.Clamp01(ai.fxHeat);
        _foam = Mathf.Clamp01(ai.fxFoam);
        _glow = Mathf.Clamp01(ai.fxGlow);
        _wave = Mathf.Clamp01(ai.fxWave);
        _spark = Mathf.Clamp01(ai.fxSpark);
        _smoke = Mathf.Clamp01(ai.fxSmoke);

        // タグで軽く補正（任意）
        if (reactionTag == "photo_explosion")
        {
            // strong, visible burst
            _spark = Mathf.Clamp01(_spark * 1.4f + 0.25f);
            _smoke = Mathf.Clamp01(_smoke * 1.2f + 0.20f);
            _glow  = Mathf.Clamp01(_glow  * 1.1f + 0.15f);
            _heat  = Mathf.Clamp01(_heat  * 1.1f + 0.10f);
        }
        else if (reactionTag == "none")
        {
            _glow *= 0.2f;
            _foam *= 0.2f;
            _smoke *= 0.2f;
        }

        Apply();

        // -------- compound particle preset --------
        int preset = 0;
        Color c = Color.white;
        if (visual != null)
        {
            preset = visual.lastParticlePreset;
            c = visual.lastSelectedColor;
        }

        // Also color the base particles (foam/smoke/spark) from the selected element/compound color.
        // This keeps particle colors consistent with ChemElementDatabase / ChemVisualController.
        ApplyBaseParticleColor(c);

        float p = Mathf.Clamp01(progress01);
        // intensity is mostly progress, but also reflect AI strengths
        float intensity = Mathf.Clamp01(0.35f * p + 0.25f * _foam + 0.25f * _smoke + 0.15f * _glow);

        ApplyCompoundParticles(preset, c, intensity);
    }


    private void ApplyBaseParticleColor(Color c)
    {
        // Keep it simple: match the element/compound color.
        ApplyParticleColor(foamParticles, c);
        ApplyParticleColor(smokeParticles, c);
        ApplyParticleColor(sparkParticles, c);
    }

    private void ApplyParticleColor(ParticleSystem ps, Color c)
    {
        if (ps == null) return;

        var main = ps.main;
        main.startColor = c;

        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (r == null) return;
        Material m = r.material;
        if (m == null) return;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    private void Apply()
    {
        // 基本粒子
        ApplyParticle(foamParticles, _foam, foamRateMax);
        ApplyParticle(smokeParticles, _smoke, smokeRateMax);
        ApplyParticle(sparkParticles, _spark, sparkRateMax);

        // 発光（EmissionColor）
        if (glowRenderers != null)
        {
            for (int i = 0; i < glowRenderers.Length; i++)
            {
                Renderer r = glowRenderers[i];
                if (r == null) continue;
                Material m = r.material;
                if (m == null) continue;
                if (m.HasProperty(emissionProperty))
                {
                    m.SetColor(emissionProperty, Color.white * (_glow * emissionMax));
                }
            }
        }

        // Heat / Wave（shader propertyがあれば）
        ApplyFloatToRenderers(heatRenderers, heatProperty, _heat * heatMax);
        ApplyFloatToRenderers(waveRenderers, waveProperty, _wave * waveMax);
    }

    private void ApplyCompoundParticles(int preset, Color baseColor, float level01)
    {
        // stop all if none
        if (preset == 0 || level01 <= 0.01f)
        {
            ApplyParticle(glintParticles, 0f, compoundRateMax);
            ApplyParticle(precipitateParticles, 0f, compoundRateMax);
            ApplyParticle(bubbleParticles, 0f, compoundRateMax);
            ApplyParticle(fogParticles, 0f, compoundRateMax);
            return;
        }

        // default stop others
        ApplyParticle(glintParticles, 0f, compoundRateMax);
        ApplyParticle(precipitateParticles, 0f, compoundRateMax);
        ApplyParticle(bubbleParticles, 0f, compoundRateMax);
        ApplyParticle(fogParticles, 0f, compoundRateMax);

        if (preset == ChemVisualController.PT_GLINT)
        {
            ApplyColoredParticle(glintParticles, baseColor, Mathf.Clamp01(level01 * 0.9f + _glow * 0.4f));
        }
        else if (preset == ChemVisualController.PT_PRECIPITATE)
        {
            // precipitate is driven by foam-ish activity
            ApplyColoredParticle(precipitateParticles, baseColor, Mathf.Clamp01(level01 * 0.8f + _foam * 0.5f));
        }
        else if (preset == ChemVisualController.PT_BUBBLE)
        {
            ApplyColoredParticle(bubbleParticles, baseColor, Mathf.Clamp01(level01 * 0.9f + _foam * 0.6f));
        }
        else if (preset == ChemVisualController.PT_FOG)
        {
            ApplyColoredParticle(fogParticles, baseColor, Mathf.Clamp01(level01 * 0.8f + _smoke * 0.6f));
        }
    }

    private void ApplyColoredParticle(ParticleSystem ps, Color c, float level01)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = c;

        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null)
        {
            Material m = r.material;
            if (m != null)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            }
        }

        ApplyParticle(ps, level01, compoundRateMax);
    }

    private void ApplyParticle(ParticleSystem ps, float level01, float rateMax)
    {
        if (ps == null) return;

        float lv = Mathf.Clamp01(level01);
        var emission = ps.emission;

        if (lv <= 0.01f)
        {
            emission.enabled = false;
            emission.rateOverTime = 0f;
            if (ps.isPlaying) ps.Stop();
            return;
        }

        emission.enabled = true;
        emission.rateOverTime = Mathf.Max(0f, rateMax) * lv;

        if (!ps.isPlaying) ps.Play();
    }


    private void ApplyFloatToRenderers(Renderer[] rs, string prop, float v)
    {
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            Material m = r.material;
            if (m == null) continue;
            if (m.HasProperty(prop))
            {
                m.SetFloat(prop, v);
            }
            else
            {
                // Fallbacks (some materials use different property names)
                if (prop == heatProperty)
                {
                    if (m.HasProperty("_HeatStrength")) m.SetFloat("_HeatStrength", v);
                    else if (m.HasProperty("_EmissionStrength")) m.SetFloat("_EmissionStrength", v);
                }
                else if (prop == waveProperty)
                {
                    if (m.HasProperty("_WaveStrength")) m.SetFloat("_WaveStrength", v);
                }
            }
        }
    }

    // =====================================================
    // Auto resolve helpers (safe defaults)
    // =====================================================
    private ParticleSystem FindParticleByNameContains(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        string ku = key.ToUpper();

        ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>(true);
        if (ps == null) return null;

        for (int i = 0; i < ps.Length; i++)
        {
            ParticleSystem p = ps[i];
            if (p == null) continue;
            string n = p.gameObject.name;
            if (string.IsNullOrEmpty(n)) continue;

            string nu = n.ToUpper();
            if (nu.IndexOf(ku) >= 0) return p;

            // a few JP keyword fallbacks for common terms
            if (ku == "FOAM" && nu.IndexOf("AWA") >= 0) return p; // 泡
            if (ku == "SMOKE" && (nu.IndexOf("KEMURI") >= 0 || nu.IndexOf("SMOKE") >= 0)) return p; // 煙
            if (ku == "SPARK" && (nu.IndexOf("HIKARI") >= 0 || nu.IndexOf("SPARK") >= 0)) return p; // 光/火花
        }

        return null;
    }

    private Renderer[] FindRenderersWithProperty(string prop)
    {
        if (string.IsNullOrEmpty(prop)) return null;

        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        if (rs == null) return null;

        // count first (Udon-friendly; avoid List allocations)
        int count = 0;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            Material m = r.sharedMaterial;
            if (m == null) continue;
            if (m.HasProperty(prop)) count++;
        }

        if (count == 0) return null;

        Renderer[] outRs = new Renderer[count];
        int j = 0;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            Material m = r.sharedMaterial;
            if (m == null) continue;
            if (m.HasProperty(prop))
            {
                outRs[j] = r;
                j++;
                if (j >= count) break;
            }
        }
        return outRs;
    }

    // =====================================================
    // Particle volume constraint (fix: particles leak outside tool)
    // =====================================================
    private void ConstrainParticlesToContainerVolume(ChemVisualController visual)
    {
        // Determine a reasonable container volume.
        // Prefer a BoxCollider named "VFXVolume" under the tool, otherwise fall back to renderer bounds.
        Transform refTr = (visual != null) ? visual.transform : transform;

        BoxCollider box = FindVfxVolumeBox(refTr);
        if (box != null)
        {
            _cachedWCenter = box.transform.TransformPoint(box.center);
            _cachedWSize = AbsVec3(box.transform.TransformVector(box.size));
        }
        else
        {
            Bounds b;
            if (!TryGetWorldBounds(refTr, out b))
            {
                _volumeConstrained = true;
                return;
            }
            _cachedWCenter = b.center;
            _cachedWSize = b.size;
        }

        // Safety clamp
        float hardMax = 0.45f;
        _cachedWSize.x = Mathf.Min(Mathf.Max(0.001f, _cachedWSize.x), hardMax);
        _cachedWSize.y = Mathf.Min(Mathf.Max(0.001f, _cachedWSize.y), hardMax);
        _cachedWSize.z = Mathf.Min(Mathf.Max(0.001f, _cachedWSize.z), hardMax);

        // Apply to every particle system we control
        ConstrainOne(foamParticles);
        ConstrainOne(smokeParticles);
        ConstrainOne(sparkParticles);
        ConstrainOne(glintParticles);
        ConstrainOne(precipitateParticles);
        ConstrainOne(bubbleParticles);
        ConstrainOne(fogParticles);

        _volumeConstrained = true;
    }

    private void ConstrainOne(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.Min(2000, Mathf.Max(64, main.maxParticles));

        Vector3 localCenter = ps.transform.InverseTransformPoint(_cachedWCenter);
        Vector3 localSize = AbsVec3(ps.transform.InverseTransformVector(_cachedWSize));

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = localCenter;
        shape.scale = localSize;

        // Kill outward push modules that often cause overflow
        var vel = ps.velocityOverLifetime; vel.enabled = false;
        var force = ps.forceOverLifetime; force.enabled = false;
        var noise = ps.noise; noise.enabled = false;
        var inherit = ps.inheritVelocity; inherit.enabled = false;

        // Clamp speed/lifetime so particles die inside
        float minDim = Mathf.Min(localSize.x, Mathf.Min(localSize.y, localSize.z));
        if (minDim < 0.001f) minDim = 0.001f;

        float maxSpeed = Mathf.Max(0.05f, minDim * 0.65f);
        if (main.startSpeed.constant > maxSpeed) main.startSpeed = maxSpeed;

        float maxLife = (minDim < 0.15f) ? 0.6f : 0.9f;
        if (main.startLifetime.constant > maxLife) main.startLifetime = maxLife;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = maxSpeed;
        limit.dampen = 0.75f;
    }

    private BoxCollider FindVfxVolumeBox(Transform t)
    {
        if (t == null) return null;

        // Search up the hierarchy a bit
        Transform p = t;
        for (int depth = 0; depth < 12 && p != null; depth++)
        {
            BoxCollider[] boxes = p.GetComponentsInChildren<BoxCollider>(true);
            if (boxes != null)
            {
                for (int i = 0; i < boxes.Length; i++)
                {
                    BoxCollider b = boxes[i];
                    if (b == null) continue;
                    string n = b.name;
                    if (n == null) n = "";
                    string u = n.ToUpper();
                    if (u == "VFXVOLUME" || u.IndexOf("VFXVOLUME") >= 0 || u.IndexOf("VOLUME") >= 0)
                        return b;
                }
            }
            p = p.parent;
        }
        return null;
    }

    private bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (root == null) return false;

        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs == null || rs.Length == 0) return false;

        bool has = false;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            if (!r.enabled) continue;
            if (!has)
            {
                bounds = r.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }
        return has;
    }

    private Vector3 AbsVec3(Vector3 v)
    {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        v.z = Mathf.Abs(v.z);
        return v;
    }

    private void EnsureParticleMaterial(ParticleSystem ps)
    {
        if (ps == null) return;
        ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
        if (pr == null) return;
        if (pr.sharedMaterial != null) return;

        Material master = GetLocalParticleMasterMaterial();
        if (master != null) pr.sharedMaterial = master;
    }

    private Material GetLocalParticleMasterMaterial()
    {
        if (_localParticleMasterMaterial != null) return _localParticleMasterMaterial;

        ParticleSystemRenderer[] prs = GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (prs == null) return null;

        for (int i = 0; i < prs.Length; i++)
        {
            ParticleSystemRenderer pr = prs[i];
            if (pr == null) continue;
            Material m = pr.sharedMaterial;
            if (m == null) continue;
            _localParticleMasterMaterial = m;
            return _localParticleMasterMaterial;
        }

        return null;
    }
}
