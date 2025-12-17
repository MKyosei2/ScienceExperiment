using UdonSharp;
using UnityEngine;

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
    public string heatProperty = "_GlowIntensity"; // shader側に無ければ無視
    [Range(0f, 5f)] public float heatMax = 1.0f;

    [Header("Wave (optional)")]
    public Renderer[] waveRenderers;
    public string waveProperty = "_WaveStrength"; // shader側に無ければ無視
    [Range(0f, 5f)] public float waveMax = 1.0f;

    // 内部強度
    private float _heat, _foam, _glow, _wave, _spark, _smoke;

    // ---- 既存互換：単発トリガー（引数は未使用でもOK）----
    public void PlayFoam(GameObject _unused) { SetFoamLevel(1f); }
    public void PlayHeat(GameObject _unused) { SetHeatLevel(1f); }
    public void PlaySpark(GameObject _unused) { SetSparkLevel(1f); }
    public void PlayWave(GameObject _unused) { SetWaveLevel(1f); }
    public void PlayGlow(GameObject _unused) { SetGlowLevel(1f); }
    public void PlaySmoke(GameObject _unused) { SetSmokeLevel(1f); }

    // ---- リアルタイム制御：0..1 ----
    public void SetHeatLevel(float v)
    {
        _heat = Clamp01(v);
        ApplyHeat();
    }

    public void SetFoamLevel(float v)
    {
        _foam = Clamp01(v);
        ApplyFoam();
    }

    public void SetGlowLevel(float v)
    {
        _glow = Clamp01(v);
        ApplyGlow();
    }

    public void SetWaveLevel(float v)
    {
        _wave = Clamp01(v);
        ApplyWave();
    }

    public void SetSparkLevel(float v)
    {
        _spark = Clamp01(v);
        ApplySpark();
    }

    public void SetSmokeLevel(float v)
    {
        _smoke = Clamp01(v);
        ApplySmoke();
    }

    private void ApplyFoam()
    {
        if (foamParticles == null) return;
        var em = foamParticles.emission;
        em.enabled = _foam > 0.01f;
        // rateOverTime は struct なのでこうする
        ParticleSystem.MinMaxCurve rate = em.rateOverTime;
        rate.constant = 50f * _foam;
        em.rateOverTime = rate;

        if (_foam > 0.01f && !foamParticles.isPlaying) foamParticles.Play();
        if (_foam <= 0.01f && foamParticles.isPlaying) foamParticles.Stop();
    }

    private void ApplySmoke()
    {
        if (smokeParticles == null) return;
        var em = smokeParticles.emission;
        em.enabled = _smoke > 0.01f;
        ParticleSystem.MinMaxCurve rate = em.rateOverTime;
        rate.constant = 30f * _smoke;
        em.rateOverTime = rate;

        if (_smoke > 0.01f && !smokeParticles.isPlaying) smokeParticles.Play();
        if (_smoke <= 0.01f && smokeParticles.isPlaying) smokeParticles.Stop();
    }

    private void ApplySpark()
    {
        if (sparkParticles == null) return;
        var em = sparkParticles.emission;
        em.enabled = _spark > 0.01f;
        ParticleSystem.MinMaxCurve rate = em.rateOverTime;
        rate.constant = 20f * _spark;
        em.rateOverTime = rate;

        if (_spark > 0.01f && !sparkParticles.isPlaying) sparkParticles.Play();
        if (_spark <= 0.01f && sparkParticles.isPlaying) sparkParticles.Stop();
    }

    private void ApplyGlow()
    {
        if (glowRenderers == null) return;
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            var r = glowRenderers[i];
            if (r == null) continue;
            var m = r.material;
            if (m == null) continue;

            // emissionがある場合だけ
            if (m.HasProperty(emissionProperty))
            {
                // 色は変えず“強度だけ”上げる（既存色に乗算）
                Color baseCol = m.GetColor(emissionProperty);
                float s = emissionMax * _glow;
                m.SetColor(emissionProperty, baseCol * Mathf.Max(0.01f, s));
            }
        }
    }

    private void ApplyHeat()
    {
        if (heatRenderers == null) return;
        float v = heatMax * _heat;

        for (int i = 0; i < heatRenderers.Length; i++)
        {
            var r = heatRenderers[i];
            if (r == null) continue;
            var m = r.material;
            if (m == null) continue;

            if (m.HasProperty(heatProperty))
            {
                m.SetFloat(heatProperty, v);
            }
        }
    }

    private void ApplyWave()
    {
        if (waveRenderers == null) return;
        float v = waveMax * _wave;

        for (int i = 0; i < waveRenderers.Length; i++)
        {
            var r = waveRenderers[i];
            if (r == null) continue;
            var m = r.material;
            if (m == null) continue;

            if (m.HasProperty(waveProperty))
            {
                m.SetFloat(waveProperty, v);
            }
        }
    }

    private float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
}
