using UnityEngine;

public class GlassRendererController : MonoBehaviour
{
    public Material materialInstance;
    public ShaderEffectData[] effects;

    void Start()
    {
        ApplyEffects();
    }

    public void ApplyEffects()
    {
        foreach (var effect in effects)
        {
            switch (effect.effectType)
            {
                case ShaderEffectType.Bubble:
                    materialInstance.SetFloat("_BubbleSpeed", effect.bubbleSpeed);
                    materialInstance.SetTexture("_BubbleNoise", effect.bubbleNoise);
                    break;
                case ShaderEffectType.Liquid:
                    materialInstance.SetFloat("_WobbleAmount", effect.liquidWobbleAmount);
                    materialInstance.SetTexture("_LiquidTex", effect.liquidTex);
                    break;
                case ShaderEffectType.Heat:
                    materialInstance.SetFloat("_HeatDistortion", effect.heatDistortionAmount);
                    materialInstance.SetTexture("_HeatWaveMap", effect.heatWaveMap);
                    break;
            }
        }
    }
}