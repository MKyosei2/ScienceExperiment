using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Environment Values")]
    public float Temperature = 25f; // °C
    public float Humidity = 40f;    // %
    public float Pressure = 101f;   // kPa

    public ChemStatusDisplay statusDisplay;

    // =============================================================
    // Modify（＋／− 調整の共通入口）
    // =============================================================
    public void Modify(string command)
    {
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

    // =============================================================
    // 値を正常範囲に収める
    // =============================================================
    private void ClampValues()
    {
        Temperature = Mathf.Clamp(Temperature, -273f, 5000f);
        Humidity = Mathf.Clamp(Humidity, 0f, 100f);
        Pressure = Mathf.Clamp(Pressure, 1f, 5000f);
    }
}
