using UdonSharp;
using UnityEngine;
using System.Text;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("References")]
    public ChemElementDatabase database;
    public ChemEnvironmentManager environment;
    public AIRequestSender ai;

    private string lastElement = "None";
    private string lastEquipment = "None";
    private StringBuilder historyLog = new StringBuilder();

    public void SelectElement(string symbol)
    {
        lastElement = symbol;
        string state = GetMatterState(symbol);
        Color col = database.GetColor(symbol);

        historyLog.AppendLine($"[Element] {symbol} ({state})");
        RequestUIRefresh();
    }

    public void SelectEquipment(string equip)
    {
        lastEquipment = equip;
        historyLog.AppendLine($"[Equipment] {equip}");
        RequestUIRefresh();
    }

    public string GetMatterState(string symbol)
    {
        float T = environment.Temperature;
        float melt = database.GetMeltingPoint(symbol);
        float boil = database.GetBoilingPoint(symbol);

        if (T < melt) return "Solid";
        if (T < boil) return "Liquid";
        return "Gas";
    }

    public void _StartExperiment()
    {
        if (lastElement == "None" || lastEquipment == "None")
        {
            historyLog.AppendLine("[ERROR] Missing element or equipment.");
            RequestUIRefresh();
            return;
        }

        historyLog.AppendLine($"[Experiment] Started → {lastElement} + {lastEquipment}");

        if (ai != null)
        {
            ai.ReceiveAIResponse("Request");
            historyLog.AppendLine("[AI] Sent reaction request.");
        }

        RequestUIRefresh();
    }

    public void _ApplyBondUpdate()
    {
        historyLog.AppendLine("[AI] Reaction complete → Compound formed.");

        RequestUIRefresh();
    }

    public string GetLastElement() => lastElement;
    public string GetLastEquipment() => lastEquipment;
    public string GetHistoryLog() => historyLog.ToString();

    private void RequestUIRefresh()
    {
        SendCustomEvent("_InternalUIRefresh");
    }

    public void _InternalUIRefresh() { }
}
