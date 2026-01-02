using UdonSharp;
using UnityEngine;

public class LiquidParticleEngine : UdonSharpBehaviour
{
    public ParticleSystem particle;
    private Material mat;
    private ParticleSystemRenderer _renderer;

    private void Start()
    {
        if (particle == null)
            particle = GetComponent<ParticleSystem>();

        _renderer = GetComponent<ParticleSystemRenderer>();
        if (_renderer != null)
            mat = _renderer.material;
    }

    public void SetColor(Color c)
    {
        if (mat != null)
        {
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        }

        if (particle != null)
        {
            var main = particle.main;
            main.startColor = c;
        }
    }

    public void SetOpacity(float a01)
    {
        if (mat != null)
        {
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(a01));
        }

        if (particle != null)
        {
            var main = particle.main;
            Color c = main.startColor.color;
            c.a = Mathf.Clamp01(a01);
            main.startColor = c;
        }
    }

    public void SetViscosity(float v)
    {
        if (mat != null && mat.HasProperty("_Viscosity"))
            mat.SetFloat("_Viscosity", v);
    }

    public void SetDensity(float d)
    {
        if (mat != null && mat.HasProperty("_Density"))
            mat.SetFloat("_Density", d);
    }

    public void SetGlow(float g)
    {
        if (mat != null && mat.HasProperty("_Glow"))
            mat.SetFloat("_Glow", g);
    }

    public void SetEmissionRate(float rate)
    {
        if (particle == null) return;
        var emission = particle.emission;
        emission.enabled = rate > 0.01f;
        emission.rateOverTime = Mathf.Max(0f, rate);
        if (rate > 0.01f)
        {
            if (!particle.isPlaying) particle.Play();
        }
        else
        {
            if (particle.isPlaying) particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void SetTurbulence(float t01)
    {
        if (particle == null) return;
        float t = Mathf.Clamp01(t01);
        var main = particle.main;
        main.startSpeed = Mathf.Lerp(0.15f, 1.5f, t);

        var vel = particle.velocityOverLifetime;
        vel.enabled = t > 0.05f;
        vel.speedModifier = Mathf.Lerp(0.5f, 2.5f, t);
    }

    /// <summary>
    /// VisualRecipe（簡易）を一括適用。
    /// bubbleRate は emissionRate として扱います。
    /// </summary>
    public void ApplyRecipe(Color baseColor, float opacity01, float viscosity, float density, float glow, float bubbleRate, float turbulence01)
    {
        Color c = baseColor;
        c.a = Mathf.Clamp01(opacity01);
        SetColor(c);
        SetOpacity(opacity01);
        SetViscosity(viscosity);
        SetDensity(density);
        SetGlow(glow);
        SetEmissionRate(Mathf.Max(0f, bubbleRate) * 20f);
        SetTurbulence(turbulence01);
    }
}
