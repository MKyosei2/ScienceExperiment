using UdonSharp;
using UnityEngine;

public enum ButtonCategory
{
    None,
    Element,
    Equipment,
    Environment
}

public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ButtonCategory category;
    public string value;

    public ChemElementSpawner spawner;
    public ChemEnvironmentManager env;
    public ChemStatusDisplay statusDisplay;

    public override void Interact()
    {
        Debug.Log($"[Button] {category} / {value}");

        switch (category)
        {
            case ButtonCategory.Element:
                if (spawner != null) spawner.SelectElement(value);
                break;

            case ButtonCategory.Equipment:
                if (spawner != null) spawner.SelectEquipment(value);
                break;

            case ButtonCategory.Environment:
                if (env != null) env.Modify(value);
                break;
        }

        if (statusDisplay != null)
            statusDisplay.RefreshUI();
    }
}
