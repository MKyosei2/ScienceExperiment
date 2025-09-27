using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("カテゴリ選択 (Inspector の enum プルダウン)")]
    public SelectionCategory category = SelectionCategory.Element;

    [Header("参照")]
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;

    [Header("Element / Compound")]
    public int elementIndex = 0;
    public bool isCompound = false; // 将来用

    [Header("Equipment")]
    public int equipmentIndex = 0;  // 今回は Conical_Flask 統一のため未使用でもOK

    [Header("Condition")]
    public bool adjustTemperature = true; // true=温度, false=圧力
    public float step = 1f;

    // 3Dオブジェクトとして「押す」
    public override void Interact() { Press(); }

    // UIイベントや他スクリプトからも呼べる
    public void Press()
    {
        switch (category)
        {
            case SelectionCategory.Element:
            case SelectionCategory.Compound:
                if (elementSpawner == null) { Debug.LogError("[SpawnSelectorButton] elementSpawner 未設定"); return; }
                elementSpawner.elementIndex = elementIndex;
                elementSpawner.isCompound = isCompound;
                elementSpawner.Spawn();
                break;

            case SelectionCategory.Condition:
                if (environmentManager == null) { Debug.LogError("[SpawnSelectorButton] environmentManager 未設定"); return; }
                if (adjustTemperature) environmentManager.AdjustTemperature(step);
                else environmentManager.AdjustPressure(step);
                break;

            case SelectionCategory.Equipment:
            case SelectionCategory.Tool:
            case SelectionCategory.Other:
            default:
                // 今回は器具も Conical_Flask に統一。必要ならここで切替を実装
                Debug.Log("[SpawnSelectorButton] category=" + category + " は現在処理なし/統一");
                break;
        }
    }
}
