using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    // ★ SelectionCategory.None は存在しないため Element を初期値に設定
    public SelectionCategory category = SelectionCategory.Element;

    public string elementSymbol = "";
    public string equipmentName = "";

    public override void Interact()
    {
        Press();
    }

    public void Press()
    {
        if (spawner == null) return;

        // ELEMENT ボタン
        if (category == SelectionCategory.Element && elementSymbol != "")
        {
            spawner.selectedElementName = elementSymbol;
            spawner.SelectElement(elementSymbol);
            return;
        }

        // TOOL ボタン
        if (category == SelectionCategory.Tool && equipmentName != "")
        {
            spawner.selectedEquipmentName = equipmentName;
            spawner.SelectEquipment(equipmentName);
            return;
        }

        // CONDITION ボタン
        if (category == SelectionCategory.Condition)
        {
            // 必要ならここに処理を書く
        }
    }
}
