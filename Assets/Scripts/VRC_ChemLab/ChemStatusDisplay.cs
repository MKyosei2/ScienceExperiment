using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemStatusDisplay : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager env;
    public TMP_Text displayText;

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (displayText == null) return;

        // 元素 & 器具
        string element = spawner != null ? spawner.GetLastElement() : "None";
        string equip = spawner != null ? spawner.GetLastEquipment() : "None";

        // 環境
        string temp = env != null ? env.Temperature.ToString("F0") : "?";
        string hum = env != null ? env.Humidity.ToString("F0") : "?";
        string pres = env != null ? env.Pressure.ToString("F0") : "?";

        // 履歴
        string hist = spawner != null ? spawner.GetHistoryLog() : "(empty)";

        displayText.text =
            $"=== Experiment Status ===\n" +
            $"Element: {element}\n" +
            $"Equipment: {equip}\n" +
            $"Temperature: {temp} °C\n" +
            $"Humidity: {hum} %\n" +
            $"Pressure: {pres} kPa\n\n" +
            $"--- Log ---\n" +
            $"{hist}";
    }
}
