using UdonSharp;
using UnityEngine;
using System.Text;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemElementDatabase db;
    public AIRequestSender ai;

    private string lastElement = "None";
    private string lastTool = "None";
    private StringBuilder log = new StringBuilder();

    public void SelectElement(string symbol)
    {
        lastElement = symbol;
        log.AppendLine($"Selected Element: {symbol}");
    }

    public void SelectEquipment(string tool)
    {
        lastTool = tool;
        log.AppendLine($"Selected Tool: {tool}");
    }

    public string GetLastElement() => lastElement;
    public string GetLastEquipment() => lastTool;
    public string GetHistoryLog() => log.ToString();

    public void _StartExperiment()
    {
        string summary = $"Experiment Started:\nElement={lastElement}, Tool={lastTool}";
        log.AppendLine(summary);

        ai.RequestAI(lastElement, lastTool, this);
    }

    public void AppendAILog(string message)
    {
        log.AppendLine(message);
    }
}
