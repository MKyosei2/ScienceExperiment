using UdonSharp;
using UnityEngine;
using TMPro;

public class ConditionDisplay : UdonSharpBehaviour
{
    public string mode; // temperature / humidity / pressure
    public ChemEnvironmentManager env;
    public TMP_Text text;

    void Update()
    {
        if (env == null || text == null) return;

        if (mode == "temperature")
            text.text = env.Temperature.ToString("F0") + " °C";

        else if (mode == "humidity")
            text.text = env.Humidity.ToString("F0") + " %";

        else if (mode == "pressure")
            text.text = env.Pressure.ToString("F2") + " atm";
    }
}
