using UdonSharp;
using UnityEngine;

public class LiquidParticleEngine : UdonSharpBehaviour
{
    public ParticleSystem particle;
    private Material mat;

    private void Start()
    {
        if (particle == null)
            particle = GetComponent<ParticleSystem>();

        ParticleSystemRenderer r = GetComponent<ParticleSystemRenderer>();
        mat = r.material;
    }

    public void SetColor(Color c)
    {
        if (mat != null)
            mat.SetColor("_Color", c);

        var main = particle.main;
        main.startColor = c;
    }

    public void SetViscosity(float v)
    {
        if (mat != null)
            mat.SetFloat("_Viscosity", v);
    }

    public void SetDensity(float d)
    {
        if (mat != null)
            mat.SetFloat("_Density", d);
    }

    public void SetGlow(float g)
    {
        if (mat != null)
            mat.SetFloat("_Glow", g);
    }
}
