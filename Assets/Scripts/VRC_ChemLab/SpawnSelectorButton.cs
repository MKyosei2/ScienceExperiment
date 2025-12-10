using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    // 外部で定義された Enum を使う
    public SelectionCategory category;
    public string value;

    public ChemElementSpawner spawner;
    public ChemEnvironmentManager env;
    public ChemStatusDisplay statusDisplay;

    public override void Interact()
    {
        if (category == SelectionCategory.Element)
        {
            spawner.SelectElement(value);
        }
        else if (category == SelectionCategory.Equipment)
        {
            spawner.SelectEquipment(value);
        }

        statusDisplay.RefreshUI();
    }
}
