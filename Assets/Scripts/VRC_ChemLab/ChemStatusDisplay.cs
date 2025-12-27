using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemStatusDisplay : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager env;
    public TextMeshProUGUI statusText;

    public void RefreshUI()
    {
        if (statusText == null) return;


if (spawner == null || env == null)
{
    statusText.text = "Experiment Status: (missing references)";
    return;
}

string e = spawner.GetLastElement();

        string t = spawner.GetLastEquipment();
        string logs = spawner.GetHistoryLog();

        statusText.text =
            $"--- Experiment Status ---\n" +
            $"Element: {e}\n" +
            $"Tool: {t}\n\n" +
            $"--- Environment ---\n" +
            $"Temp: {env.Temperature} °C\n" +
            $"Humidity: {env.Humidity} %\n" +
            $"Pressure: {env.Pressure} kPa\n\n" +
            $"--- Logs ---\n" +
            logs;
    }
}
