using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemStatusDisplay : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager environment;
    public TMP_Text displayText;

    public void RefreshUI()
    {
        if (displayText == null) return;

        string e = spawner != null ? spawner.GetLastElement() : "None";
        string eq = spawner != null ? spawner.GetLastEquipment() : "None";
        string logs = spawner != null ? spawner.GetHistoryLog() : "";

        string temp = environment != null ? environment.Temperature.ToString("F0") : "?";
        string hum = environment != null ? environment.Humidity.ToString("F0") : "?";
        string pres = environment != null ? environment.Pressure.ToString("F0") : "?";

        displayText.text =
            $"=== Experiment Status ===\n" +
            $"Element: {e}\n" +
            $"Equipment: {eq}\n" +
            $"Temperature: {temp} °C\n" +
            $"Humidity: {hum} %\n" +
            $"Pressure: {pres} kPa\n" +
            $"--- Log ---\n{logs}";
    }
}
