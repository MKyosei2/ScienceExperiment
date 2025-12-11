using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("Button Settings")]
    public SelectionCategory category = SelectionCategory.Element;

    // ボタン ID（元素記号・器具名・条件名）
    public string idOrName;

    [Header("Reference Targets")]
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;
    public ChemStatusDisplay statusDisplay;

    public override void Interact()
    {
        HandlePress();
    }

    public void HandlePress()
    {
        if (string.IsNullOrEmpty(idOrName))
        {
            Debug.LogWarning($"[SpawnSelectorButton] {name} の idOrName が設定されていません。");
            return;
        }

        switch (category)
        {
            case SelectionCategory.Element:
                if (elementSpawner != null)
                {
                    elementSpawner.SelectElement(idOrName);
                    Debug.Log($"Element Selected: {idOrName}");
                }
                break;

            case SelectionCategory.Tool:
                if (elementSpawner != null)
                {
                    elementSpawner.SelectEquipment(idOrName);
                    Debug.Log($"Equipment Selected: {idOrName}");
                }
                break;

            case SelectionCategory.Condition:
                if (environmentManager != null)
                {
                    // TEMP+, TEMP-, HUMID+, PRESS- 等を自動解釈
                    environmentManager.Modify(idOrName);
                    Debug.Log($"Condition Modified: {idOrName}");
                }
                break;
        }

        if (statusDisplay != null)
            statusDisplay.RefreshUI();
    }
}
