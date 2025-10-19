using UdonSharp;
using UnityEngine;
using VRC.Udon;

[AddComponentMenu("VRC Lab/ValueAdjustButton")]
public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager envManager;
    public string type; // "Temperature", "Pressure", "Humidity"
    public float step = 1f;

    public void _OnClick()
    {
        if (envManager == null) return;

        if (type == "Temperature")
            envManager.SendCustomEvent("_AdjustTemperature");
        else if (type == "Pressure")
            envManager.SendCustomEvent("_AdjustPressure");
        else if (type == "Humidity")
            envManager.SendCustomEvent("_AdjustHumidity");
    }
}
