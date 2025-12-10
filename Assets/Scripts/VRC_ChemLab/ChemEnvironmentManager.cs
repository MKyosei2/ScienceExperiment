using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    public float Temperature = 25f;
    public float Pressure = 1f;
    public float Humidity = 50f;

    // -------------------------
    // 新バージョン基本制御
    // -------------------------
    public void _ChangeTemperature(float delta)
    {
        Temperature += delta;
        Debug.Log("[ENV] Temperature = " + Temperature);
    }

    public void _ChangePressure(float delta)
    {
        Pressure += delta;
        Debug.Log("[ENV] Pressure = " + Pressure);
    }

    public void _ChangeHumidity(float delta)
    {
        Humidity += delta;
        Debug.Log("[ENV] Humidity = " + Humidity);
    }

    // -------------------------
    // Modify 統一制御（3Dボタン用）
    // -------------------------
    public void Modify(string key)
    {
        switch (key)
        {
            case "TempUp": _ChangeTemperature(+1); break;
            case "TempDown": _ChangeTemperature(-1); break;

            case "HumUp": _ChangeHumidity(+1); break;
            case "HumDown": _ChangeHumidity(-1); break;

            case "PressUp": _ChangePressure(+1); break;
            case "PressDown": _ChangePressure(-1); break;

            default:
                Debug.LogWarning("[ENV] Unknown key: " + key);
                break;
        }
    }

    // ==============================
    // ▼ UI 互換（旧スクリプト互換）
    // ==============================
    public void _AdjustTemperature(float delta) { _ChangeTemperature(delta); }
    public void _AdjustHumidity(float delta) { _ChangeHumidity(delta); }
    public void _AdjustPressure(float delta) { _ChangePressure(delta); }

    public void _SetTemperature(float v) { Temperature = v; }
    public void _SetPressure(float v) { Pressure = v; }
    public void _SetHumidity(float v) { Humidity = v; }
}
