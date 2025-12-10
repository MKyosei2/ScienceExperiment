using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Environment Values")]
    public float Temperature = 25f;   // °C
    public float Humidity = 50f;      // %
    public float Pressure = 1f;       // kPa

    // ======== 調整処理 ========
    public void AdjustTemperature(float step)
    {
        Temperature += step;
    }

    public void AdjustHumidity(float step)
    {
        Humidity += step;
    }

    public void AdjustPressure(float step)
    {
        Pressure += step;
    }
}
