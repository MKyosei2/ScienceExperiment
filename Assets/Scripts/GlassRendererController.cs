using UnityEngine;
using System.IO;

[ExecuteAlways]
public class GlassRendererController : MonoBehaviour
{
    public Material materialInstance;
    public ShaderEffectData[] effects; // この配列をJSONから変更可能にする

    [System.Serializable]
    public class ShaderJsonOverride
    {
        public bool useBubble = false;
        public bool useLiquid = false;
        public bool useHeat = false;

        public float bubbleSpeed;
        public float liquidWobbleAmount;
        public float heatDistortionAmount;

        public string liquidTex;
        public string bubbleNoise;
        public string heatWaveMap;

        public float[] mainColor;
    }

    void Start()
    {
        ApplyEffects();

#if UNITY_EDITOR
        LoadJsonReaction();  // Editor再生時に反映
#endif
    }

#if UNITY_EDITOR
    void LoadJsonReaction()
    {
        string path = Application.streamingAssetsPath + "/experiment_result.json";
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        ShaderJsonOverride overrideData = JsonUtility.FromJson<ShaderJsonOverride>(json);
        if (overrideData == null) return;

        var list = new System.Collections.Generic.List<ShaderEffectData>();

        foreach (var effect in effects)
        {
            if (effect == null) continue;

            switch (effect.effectType)
            {
                case ShaderEffectType.Bubble:
                    if (!overrideData.useBubble) continue;
                    effect.bubbleSpeed = overrideData.bubbleSpeed;
                    effect.bubbleNoise = LoadTex(overrideData.bubbleNoise);
                    break;

                case ShaderEffectType.Liquid:
                    if (!overrideData.useLiquid) continue;
                    effect.liquidWobbleAmount = overrideData.liquidWobbleAmount;
                    effect.liquidTex = LoadTex(overrideData.liquidTex);
                    break;

                case ShaderEffectType.Heat:
                    if (!overrideData.useHeat) continue;
                    effect.heatDistortionAmount = overrideData.heatDistortionAmount;
                    effect.heatWaveMap = LoadTex(overrideData.heatWaveMap);
                    break;
            }

            list.Add(effect); // 有効なものだけ追加
        }

        effects = list.ToArray(); // 絞り込んだ配列を再代入

        if (overrideData.mainColor != null && overrideData.mainColor.Length == 4)
        {
            Color mc = new Color(
                overrideData.mainColor[0],
                overrideData.mainColor[1],
                overrideData.mainColor[2],
                overrideData.mainColor[3]
            );
            materialInstance.SetColor("_MainColor", mc);
        }

        ApplyEffects(); // 再適用
    }

    Texture2D LoadTex(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return null;
        string filename = Path.GetFileNameWithoutExtension(nameOrPath);
        return Resources.Load<Texture2D>(filename);
    }
#endif

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
