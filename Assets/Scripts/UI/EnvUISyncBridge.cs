using UdonSharp;
using UnityEngine;
using TMPro;

[AddComponentMenu("VRC Lab/EnvUISyncBridge")]
public class EnvUISyncBridge : UdonSharpBehaviour
{
    public ChemEnvironmentManager manager;
    public TMP_InputField tempField;
    public TMP_InputField pressField;
    public TMP_InputField humidField;

    public void _RefreshAllDisplays()
    {
        if (manager == null) return;
        tempField.text = manager.Temperature.ToString("F1");
        pressField.text = manager.Pressure.ToString("F1");
        humidField.text = manager.Humidity.ToString("F1");
    }

    public void _SetTemperature(string value)
    {
        if (manager == null) return;
        if (float.TryParse(value, out float t))
            manager._SetTemperature(t);
    }

    public void _SetPressure(string value)
    {
        if (manager == null) return;
        if (float.TryParse(value, out float p))
            manager._SetPressure(p);
    }

    public void _SetHumidity(string value)
    {
        if (manager == null) return;
        if (float.TryParse(value, out float h))
            manager._SetHumidity(h);
    }
}
