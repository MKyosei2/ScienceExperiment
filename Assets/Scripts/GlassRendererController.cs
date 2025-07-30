using UnityEngine;

[ExecuteAlways]
public class GlassRendererController : MonoBehaviour
{
    public Material materialInstance;
    public ShaderEffectData[] effects;

    void Start() => ApplyEffects();
#if UNITY_EDITOR
    void OnValidate() => ApplyEffects();
#endif

    public void ApplyEffects()
    {
        if (materialInstance == null || effects == null) return;

        foreach (var effect in effects)
        {
            if (effect == null) continue;

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

        UpdateMeshProperties();
    }

    private void UpdateMeshProperties()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        Bounds bounds = mr.bounds;
        materialInstance.SetVector("_MeshCenter", bounds.center);
        materialInstance.SetVector("_MeshSize", bounds.size);
    }
}
