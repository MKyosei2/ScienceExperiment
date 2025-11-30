using UdonSharp;
using UnityEngine;

public class ChemReactionAnimator : UdonSharpBehaviour
{
    public ParticleSystem foam;       // 泡
    public ParticleSystem heat;       // 熱ゆらぎ
    public ParticleSystem spark;      // 火花
    public Renderer glowRenderer;     // 発光用モデル
    public LiquidSurfaceController liquidSurface; // 波紋

    public void PlayFoam(ParticleSystem target)
    {
        if (foam != null) foam.Play();
    }

    public void PlayHeat(ParticleSystem target)
    {
        if (heat != null) heat.Play();
    }

    public void PlaySpark(ParticleSystem target)
    {
        if (spark != null) spark.Play();
    }

    public void PlayGlow(GameObject obj)
    {
        if (glowRenderer != null)
        {
            Material m = glowRenderer.material;
            m.SetFloat("_EmissionGain", 1.2f);
            SendCustomEventDelayedSeconds("_StopGlow", 0.5f);
        }
    }

    public void _StopGlow()
    {
        if (glowRenderer != null)
        {
            glowRenderer.material.SetFloat("_EmissionGain", 0f);
        }
    }

    public void PlayWave(ParticleSystem p)
    {
        if (liquidSurface != null)
        {
            liquidSurface.SetRipple(0.4f);
            SendCustomEventDelayedSeconds("_StopRipple", 0.6f);
        }
    }

    public void _StopRipple()
    {
        if (liquidSurface != null)
            liquidSurface.SetRipple(0f);
    }
}
