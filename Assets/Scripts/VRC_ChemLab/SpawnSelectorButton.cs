using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/SpawnSelectorButton")]
public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    // "Element" または "Equipment"
    public string type;

    // このボタンが担当する元素名 or 器具名
    public string targetName;

    // カテゴリー情報（未使用でも残す）
    public SelectionCategory category;
    public string categoryName;

    public override void Interact()
    {
        Press();
    }

    public void Press()
    {
        _OnClick();
    }

    public void OnClick()
    {
        _OnClick();
    }

    public void _OnClick()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[SpawnSelectorButton] spawner 未設定");
            return;
        }

        // ==============================
        // ELEMENT
        // ==============================
        if (type == "Element")
        {
            spawner.selectedElementName = targetName;

            // ★ 新仕様：引数付きゲートを呼ぶ
            // これが最も確実に ChemElementSpawner を動作させる
            spawner.SelectElement(targetName);

            Debug.Log("[SpawnSelectorButton] Element Pressed: " + targetName);
            return;
        }

        // ==============================
        // EQUIPMENT
        // ==============================
        if (type == "Equipment")
        {
            spawner.selectedEquipmentName = targetName;

            // ★ 新仕様：装置側の設定も正しく呼ぶ
            spawner.SelectEquipment(targetName);

            Debug.Log("[SpawnSelectorButton] Equipment Pressed: " + targetName);
            return;
        }

        Debug.LogWarning("[SpawnSelectorButton] type が不正（Element / Equipment ではありません）");
    }
}
