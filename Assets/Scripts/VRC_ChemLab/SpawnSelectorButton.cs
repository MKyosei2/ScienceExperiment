using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    public ExperimentOrchestrator orchestrator;
    public ChemElementSpawner elementSpawner;

    [Header("ボタン設定")]
    public string elementSymbol;
    public bool isEquipmentButton;
    public bool isStartButton;
    public bool isResetButton;

    // ← CategoryControllerが参照しているフィールドを追加
    [Header("カテゴリ設定")]
    public SelectionCategory category; // CategoryControllerで利用されるenum型

    public void Press() => Interact();

    public override void Interact()
    {
        if (isStartButton && orchestrator)
        {
            orchestrator.StartExperiment();
            return;
        }

        if (isResetButton && orchestrator)
        {
            orchestrator.ResetExperiment();
            return;
        }

        if (isEquipmentButton && elementSpawner)
        {
            elementSpawner.SelectEquipment();
            return;
        }

        if (!string.IsNullOrEmpty(elementSymbol) && elementSpawner)
        {
            elementSpawner.SelectElement(elementSymbol);
        }
    }
}