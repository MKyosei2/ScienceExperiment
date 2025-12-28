using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemReactionAnimator (Async VFX controller)
/// spawner / AIRequestSender が出した係数を、粒子・発光・液面などに適用する。
/// </summary>
public class ChemReactionAnimator : UdonSharpBehaviour
{
    [Header("Particles (optional)")]
    public ParticleSystem foamParticles;
    public ParticleSystem smokeParticles;
    public ParticleSystem sparkParticles;

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
    }

    /// <summary>
    /// reactionTag と ai の係数からプリセット適用
    /// </summary>
    public void ApplyPreset(string reactionTag, AIRequestSender ai, float progress01)
    {
        if (ai == null)
        {
            ResetLevels();
            return;
        }

        // 基本はai係数を使う。reactionTagが無い場合もこれで動く。
        SetHeatLevel(ai.fxHeat);
        SetFoamLevel(ai.fxFoam);
        SetGlowLevel(ai.fxGlow);
        SetWaveLevel(ai.fxWave);
        SetSparkLevel(ai.fxSpark);
        SetSmokeLevel(ai.fxSmoke);

        // タグで軽く補正（任意）
        if (reactionTag == "none")
        {
            // 変化が弱い
            SetGlowLevel(ai.fxGlow * 0.2f);
            SetFoamLevel(ai.fxFoam * 0.2f);
            SetSmokeLevel(ai.fxSmoke * 0.2f);
        }
    }

    private void Apply()
    {
        // 粒子（量はEmissionRateでなく、Play/Stopの切替＋rateOverTimeを触れるなら触る）
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

    private void ApplyParticle(ParticleSystem ps, float level01)
    {
        if (ps == null) return;
        if (level01 <= 0.01f)
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return;
        }

        if (!ps.isPlaying) ps.Play();
        // 可能ならrateOverTimeを調整（Main/EmissionはUdonで触れるが、環境次第で無理なら無視）
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f * level01;
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
