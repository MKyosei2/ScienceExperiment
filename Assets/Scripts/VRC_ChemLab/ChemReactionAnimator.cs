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
        ApplyParticle(glintParticles, 0f);
        ApplyParticle(precipitateParticles, 0f);
        ApplyParticle(bubbleParticles, 0f);
        ApplyParticle(fogParticles, 0f);
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
        if (reactionTag == "none")
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

        float p = Mathf.Clamp01(progress01);
        // intensity is mostly progress, but also reflect AI strengths
        float intensity = Mathf.Clamp01(0.35f * p + 0.25f * _foam + 0.25f * _smoke + 0.15f * _glow);

        ApplyCompoundParticles(preset, c, intensity);
    }

    private void Apply()
    {
        // 基本粒子
        ApplyParticle(foamParticles, _foam);
        ApplyParticle(smokeParticles, _smoke);
        ApplyParticle(sparkParticles, _spark);

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
            ApplyParticle(glintParticles, 0f);
            ApplyParticle(precipitateParticles, 0f);
            ApplyParticle(bubbleParticles, 0f);
            ApplyParticle(fogParticles, 0f);
            return;
        }

        // default stop others
        ApplyParticle(glintParticles, 0f);
        ApplyParticle(precipitateParticles, 0f);
        ApplyParticle(bubbleParticles, 0f);
        ApplyParticle(fogParticles, 0f);

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

        ApplyParticle(ps, level01);
    }

    private void ApplyParticle(ParticleSystem ps, float level01)
    {
        if (ps == null) return;
        if (level01 <= 0.01f)
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return;
        }

        if (!ps.isPlaying) ps.Play();
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f * Mathf.Clamp01(level01);
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
            if (m.HasProperty(prop)) m.SetFloat(prop, v);
        }
    }
}
