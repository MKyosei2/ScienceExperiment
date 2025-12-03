using UdonSharp;
using UnityEngine;

<<<<<<< Updated upstream
[AddComponentMenu("VRC Lab/SpawnSelectorButton")]
=======
public enum ButtonCategory
{
    Element,
    Equipment,
    Environment
}

>>>>>>> Stashed changes
public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("設定")]
    public ButtonCategory category = ButtonCategory.Element;

    [Header("参照")]
    public ChemElementSpawner spawner;

<<<<<<< Updated upstream
    // "Element" または "Equipment"
    public string type;

    // このボタンが担当する元素名 or 器具名
    public string targetName;

    // カテゴリー情報（未使用でも残す）
    public SelectionCategory category;
    public string categoryName;
=======
    [Header("ボタン内容")]
    public string elementSymbol = "";
    public string equipmentName = "";
    public string environmentCommand = "";
>>>>>>> Stashed changes

    public override void Interact()
    {
        Debug.Log("[Button] 押された: " + GetButtonDescription());

        if (spawner == null)
        {
            Debug.LogError("[Button] spawner が設定されていません");
            return;
        }

        if (category == ButtonCategory.Element)
        {
            Debug.Log("[Button] Element: " + elementSymbol);
            spawner.SelectElement(elementSymbol);
        }
        else if (category == ButtonCategory.Equipment)
        {
            Debug.Log("[Button] Equipment: " + equipmentName);
            spawner.SelectEquipment(equipmentName);
        }
        else if (category == ButtonCategory.Environment)
        {
            Debug.Log("[Button] Environment: " + environmentCommand);
            spawner.SendCustomEvent(environmentCommand);
        }
    }

    private string GetButtonDescription()
    {
<<<<<<< Updated upstream
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
=======
        if (category == ButtonCategory.Element)
            return "元素 (" + elementSymbol + ")";

        if (category == ButtonCategory.Equipment)
            return "器具 (" + equipmentName + ")";

        if (category == ButtonCategory.Environment)
            return "環境 (" + environmentCommand + ")";

        return "未設定ボタン";
>>>>>>> Stashed changes
    }
}
