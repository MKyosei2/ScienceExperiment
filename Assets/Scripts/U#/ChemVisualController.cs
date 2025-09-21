// ==================================
// ChemVisualController.cs
// UdonSharp で安全に動作するバージョン
// Inspector に elementMaterials を割り当て、要素ごとに差し替える
// ==================================

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemVisualController : UdonSharpBehaviour
{
    [Header("▼ 元素ごとのマテリアル (WireframeFX.shader を使ったものをアサイン)")]
    [Tooltip("UIボタンの index に対応して割り当て。未設定 index は defaultMaterial を使用")]
    public Material[] elementMaterials;

    [Header("▼ デフォルトマテリアル")]
    public Material defaultMaterial;

    /// <summary>
    /// 指定のフラスコに、元素のマテリアルを適用する
    /// </summary>
    public void ApplyElementVisual(GameObject conicalFlaskRoot, int elementId)
    {
        if (conicalFlaskRoot == null) return;

        Material mat = defaultMaterial;
        if (elementId >= 0 && elementId < elementMaterials.Length && elementMaterials[elementId] != null)
        {
            mat = elementMaterials[elementId];
        }

        MeshRenderer[] renders = conicalFlaskRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (renders == null || renders.Length == 0) return;

        for (int r = 0; r < renders.Length; r++)
        {
            MeshRenderer mr = renders[r];
            if (mr == null) continue;

            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                shared[i] = mat;
            }
            mr.sharedMaterials = shared;
        }
    }

    /// <summary>
    /// 液体を消す（リセット時に呼ぶ）
    /// </summary>
    public void ClearLiquid(GameObject conicalFlaskRoot)
    {
        if (conicalFlaskRoot == null) return;
        MeshRenderer[] renders = conicalFlaskRoot.GetComponentsInChildren<MeshRenderer>(true);
        if (renders == null) return;

        for (int r = 0; r < renders.Length; r++)
        {
            MeshRenderer mr = renders[r];
            if (mr == null) continue;

            Material[] mats = mr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = defaultMaterial;
            }
            mr.sharedMaterials = mats;
        }
    }
}
