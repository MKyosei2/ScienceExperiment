using UdonSharp;
using UnityEngine;
using VRC.Udon;

[AddComponentMenu("VRC Lab/SpawnSelectorButton")]
public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    [Header("ボタンタイプ設定")]
    [Tooltip("Button Type: Equipment / Element")]
    public string type; // "Equipment" or "Element"

    [Tooltip("このボタンが対応する対象名 (例: 'Hydrogen' or 'ConicalFlask')")]
    public string targetName;

    [Header("カテゴリ設定")]
    [Tooltip("このボタンが属するカテゴリ (SelectionCategory列挙型)")]
    public SelectionCategory category;

    // --- ボタン押下時に呼ばれる処理 ---
    public void _OnClick()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[SpawnSelectorButton] Spawnerが設定されていません。");
            return;
        }

        if (type == "Equipment")
        {
            spawner.selectedEquipmentName = targetName; // ← public化済み
            spawner.SendCustomEvent("_SelectEquipment");
            Debug.Log($"[SpawnSelectorButton] 器具 '{targetName}' を選択 (カテゴリ: {category})");
        }
        else if (type == "Element")
        {
            spawner.selectedElementName = targetName; // ← public化済み
            spawner.SendCustomEvent("_SelectElement");
            Debug.Log($"[SpawnSelectorButton] 元素 '{targetName}' を選択 (カテゴリ: {category})");
        }
    }

    /// <summary>
    /// 他スクリプトからボタン押下を再現する用（SelectionActionController互換）
    /// </summary>
    public void Press()
    {
        _OnClick();
    }
}
