using UdonSharp;
using UnityEngine;
using System.Text;

public class ChemElementSpawner : UdonSharpBehaviour
{
    private string lastElement = "None";
    private string lastEquipment = "None";

    private StringBuilder historyLog = new StringBuilder();

    // --- 元素選択 ---
    public void SelectElement(string symbol)
    {
        lastElement = symbol;
        historyLog.AppendLine($"Selected Element: {symbol}");
    }

    // --- 実験器具選択 ---
    public void SelectEquipment(string equip)
    {
        lastEquipment = equip;
        historyLog.AppendLine($"Selected Equipment: {equip}");
    }

    // --- Getter ---
    public string GetLastElement() => lastElement;
    public string GetLastEquipment() => lastEquipment;

    public string GetHistoryLog()
    {
        return historyLog.Length > 0 ? historyLog.ToString() : "(empty)";
    }

    // --- 実験開始時のログ追加（StartExperimentButton から呼ばれる） ---
    public void _StartExperiment()
    {
        historyLog.AppendLine($"Experiment Started with [{lastElement}] + [{lastEquipment}]");
    }
}
