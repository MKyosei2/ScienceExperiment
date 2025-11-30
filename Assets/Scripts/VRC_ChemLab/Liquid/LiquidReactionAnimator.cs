using UdonSharp;
using UnityEngine;

public class LiquidReactionAnimator : UdonSharpBehaviour
{
    public LiquidSurfaceController surface;
    public LiquidWaveController wave;
    public LiquidBoilingController boil;
    public LiquidParticleEngine particleEngine;

    // 泡・発泡
    public void PlayFoam()
    {
        if (boil != null)
            boil.PlayBoil();
    }

    // 熱反応
    public void PlayHeat()
    {
        if (boil != null)
            boil.PlayHeat();
    }

    // 金属反応 → 波紋 + 輝き
    public void PlaySpark()
    {
        if (surface != null)
            surface.PulseColor(new Color(1, 1, 0.5f));
    }

    // 発光（Glow）
    public void PlayGlow()
    {
        if (particleEngine != null)
            particleEngine.SetGlow(1.0f);
    }

    // 波反応
    public void PlayWave()
    {
        if (wave != null)
            wave.AddWave(0.3f);
    }
}
