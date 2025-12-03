using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public enum ButtonCategory
{
    Element,
    Equipment,
    Environment
}

public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("設定")]
    public ButtonCategory category = ButtonCategory.Element;

    [Header("参照")]
    public ChemElementSpawner spawner;

    [Header("ボタン内容")]
    public string elementSymbol = "";
    public string equipmentName = "";
    public string environmentCommand = "";

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
        if (category == ButtonCategory.Element)
            return "元素 (" + elementSymbol + ")";

        if (category == ButtonCategory.Equipment)
            return "器具 (" + equipmentName + ")";

        if (category == ButtonCategory.Environment)
            return "環境 (" + environmentCommand + ")";

        return "未設定ボタン";
    }
}
