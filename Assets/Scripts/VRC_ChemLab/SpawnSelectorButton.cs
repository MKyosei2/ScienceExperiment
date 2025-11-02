using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/SpawnSelectorButton")]
public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    // "Element" or "Equipment"
    public string type;

    // 押したときにSpawnerに渡す名前（元素名とか、器具名とか）
    public string targetName;

    // ← これが CategoryController から参照されてる
    public SelectionCategory category;   // enum でカテゴリを管理してる場合はこちらを使う

    // ← もし文字列で見に来てるならこっちを使ってもらう
    public string categoryName;

    // 3Dボタンをプレイヤーが押したとき
    public override void Interact()
    {
        Press();
    }

    // SelectionActionController がここを呼んでくるので必ず残す
    public void Press()
    {
        _OnClick();
    }

    // 旧式のUIボタンから呼びたい場合用
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

        if (type == "Element")
        {
            spawner.selectedElementName = targetName;
            spawner.SendCustomEvent("_SelectElement");
        }
        else if (type == "Equipment")
        {
            spawner.selectedEquipmentName = targetName;
            spawner.SendCustomEvent("_SelectEquipment");
        }
        else
        {
            Debug.LogWarning("[SpawnSelectorButton] type が Element / Equipment ではありません");
        }
    }
}
