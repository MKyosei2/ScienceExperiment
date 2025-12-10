using UdonSharp;
using UnityEngine;
using System.Text;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("UI への通知")]
    public ChemStatusDisplay statusDisplay;
    public ChemEnvironmentManager environment;

    private string lastElement = "None";
    private string lastEquipment = "None";

    private StringBuilder historyLog = new StringBuilder();

    // ================================
    // 元素選択
    // ================================
    public void SelectElement(string symbol)
    {
        lastElement = symbol;
        historyLog.AppendLine($"Selected Element: {symbol}");

        RefreshUI();
    }

    // ================================
    // 実験器具選択
    // ================================
    public void SelectEquipment(string equip)
    {
        lastEquipment = equip;
        historyLog.AppendLine($"Selected Equipment: {equip}");

        RefreshUI();
    }

    // ================================
    // 実験開始ログ
    // ================================
    public void _StartExperiment()
    {
        historyLog.AppendLine($"Experiment Started with [{lastElement}] + [{lastEquipment}]");

        RefreshUI();
    }

    // ================================
    // UI へ反映
    // ================================
    private void RefreshUI()
    {
        if (statusDisplay != null)
        {
            statusDisplay.RefreshUI();
        }
    }

    // ================================
    // Getter
    // ================================
    public string GetLastElement() => lastElement;
    public string GetLastEquipment() => lastEquipment;

    public string GetHistoryLog()
    {
        return historyLog.Length > 0 ? historyLog.ToString() : "(empty)";
    }
}
