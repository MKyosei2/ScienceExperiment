using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/SpawnSelectorButton")]
public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public string type;        // "Element" or "Equipment"
    public string targetName;  // 押したときに spawner に渡す名前
    public SelectionCategory category;

    // 3Dモデルをプレイヤーが押したとき（Eキー・トリガー）
    public override void Interact()
    {
        _OnClick();
    }

    // ← SelectionActionController がここを呼んでくるので残す
    public void Press()
    {
        _OnClick();
    }

    // 旧UI系や他のスクリプトから呼べるようにもしておく
    public void OnClick()
    {
        _OnClick();
    }

    // 実際の処理はここだけ
    public void _OnClick()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[SpawnSelectorButton] spawner 未設定");
            return;
        }

        if (type == "Element")
        {
            spawner.selectedElementName = targetName;
            spawner.SendCustomEvent("_SelectElement");
            // Debug.Log($"[SpawnSelectorButton] 元素 '{targetName}' を選択しました。");
        }
        else if (type == "Equipment")
        {
            spawner.selectedEquipmentName = targetName;
            spawner.SendCustomEvent("_SelectEquipment");
            // Debug.Log($"[SpawnSelectorButton] 器具 '{targetName}' を選択しました。");
        }
        else
        {
            Debug.LogWarning("[SpawnSelectorButton] type が Element / Equipment ではありません");
        }
    }
}
