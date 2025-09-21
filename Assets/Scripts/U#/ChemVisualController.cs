using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemVisualController : UdonSharpBehaviour
{
    [Header("▼ 共通ベースマテリアル (WireframeFX.shader をアサイン)")]
    public Material sharedLiquidMaterial;

    [Header("▼ 元素ごとの色")]
    public Color[] elementColors;
    public Color defaultElementColor = Color.white;

    [Header("▼ 液体の共通パラメータ")]
    public float baseFoamWidth = 0.02f;
    public float baseTurbidity = 0.1f;
    public float baseHeatAmount = 0.0f;

    public void ApplyElementVisual(GameObject flaskRoot, int elementId, float fill, float alpha)
    {
        if (flaskRoot == null || sharedLiquidMaterial == null) return;

        // 元素の色
        Color liquid = defaultElementColor;
        if (elementId >= 0 && elementId < elementColors.Length)
            liquid = elementColors[elementId];

        MeshRenderer[] renders = flaskRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (renders == null) return;

        // 共通PropertyBlock設定
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_LiquidColor", liquid);
        mpb.SetFloat("_LiquidAlpha", Mathf.Clamp01(alpha));
        mpb.SetFloat("_FillLevel", Mathf.Clamp01(fill));
        mpb.SetFloat("_FoamWidth", baseFoamWidth);
        mpb.SetFloat("_Turbidity", baseTurbidity);
        mpb.SetFloat("_HeatAmount", baseHeatAmount);

        for (int r = 0; r < renders.Length; r++)
        {
            MeshRenderer mr = renders[r];
            if (mr == null) continue;
            mr.sharedMaterial = sharedLiquidMaterial;
            mr.SetPropertyBlock(mpb);
        }

        // 複数の挙動スクリプトに通知
        FlaskBehaviour[] behaviours = flaskRoot.GetComponentsInChildren<FlaskBehaviour>(true);
        if (behaviours != null)
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].OnElementApplied(elementId);
                }
            }
        }
    }
}
