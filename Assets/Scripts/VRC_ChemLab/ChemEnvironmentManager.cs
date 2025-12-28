using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemEnvironmentManager
/// ・UIからの温度/湿度/圧力操作の受け口
/// ・同期の真実は spawner 側に持たせる想定（ここは値の保持）
/// </summary>
public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Environment Values")]
    public float Temperature = 25f; // °C
    public float Humidity = 40f;    // %
    public float Pressure = 101f;   // kPa

    [Header("Defaults")]
    public float defaultTemperature = 25f;
    public float defaultHumidity = 40f;
    public float defaultPressure = 101f;

    public ChemStatusDisplay statusDisplay;

    // alias (for newer code)
    public float temperatureC { get { return Temperature; } }
    public float humidity { get { return Humidity; } }
    public float pressureKPa { get { return Pressure; } }

    // =============================================================
    // Modify（＋／− 調整の共通入口）
    // =============================================================
    public void Modify(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        if (command == "TempUp") Temperature += 1f;
        else if (command == "TempDown") Temperature -= 1f;

        else if (command == "HumUp") Humidity += 1f;
        else if (command == "HumDown") Humidity -= 1f;

        else if (command == "PresUp") Pressure += 1f;
        else if (command == "PresDown") Pressure -= 1f;

        ClampValues();

        if (statusDisplay != null)
            statusDisplay.RefreshUI();
    }

    public void SetValues(float tempC, float pressureKPaValue, float humidityPct)
    {
        Temperature = tempC;
        Pressure = pressureKPaValue;
        Humidity = humidityPct;
        ClampValues();
    }

    public void _ResetToDefaults()
    {
        Temperature = defaultTemperature;
        Humidity = defaultHumidity;
        Pressure = defaultPressure;
        ClampValues();

        if (statusDisplay != null)
            statusDisplay.RefreshUI();
    }

    private void ClampValues()
    {
        Temperature = Mathf.Clamp(Temperature, -50f, 500f);
        Humidity = Mathf.Clamp(Humidity, 0f, 100f);
        Pressure = Mathf.Clamp(Pressure, 50f, 300f);
    }
}
