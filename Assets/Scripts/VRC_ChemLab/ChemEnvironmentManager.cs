using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/ChemEnvironmentManager")]
public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [UdonSynced] public float Temperature = 25f;
    [UdonSynced] public float Pressure = 1f;
    [UdonSynced] public float Humidity = 50f;

    // ==== 旧呼び出し互換 ====
    public void AdjustTemperature(float d) { _AdjustTemperature(d); }
    public void AdjustPressure(float d) { _AdjustPressure(d); }
    public void AdjustHumidity(float d) { _AdjustHumidity(d); }

    public void _ResetToDefaults()
    {
        Temperature = 25f;
        Pressure = 1f;
        Humidity = 50f;
        Debug.Log("[EnvManager] ResetToDefaults()");
    }

    public void _SetTemperature(float v) { Temperature = v; }
    public void _SetPressure(float v) { Pressure = v; }
    public void _SetHumidity(float v) { Humidity = Mathf.Clamp(v, 0f, 100f); }

    public void _AdjustTemperature(float delta) { Temperature += delta; }
    public void _AdjustPressure(float delta) { Pressure += delta; }
    public void _AdjustHumidity(float delta) { Humidity = Mathf.Clamp(Humidity + delta, 0f, 100f); }
}
