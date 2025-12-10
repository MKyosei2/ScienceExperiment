using UdonSharp;
using UnityEngine;
using TMPro;

public class EnvUISyncBridge : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public TMP_Text tempText;
    public TMP_Text humText;
    public TMP_Text presText;

    public void RefreshUI()
    {
        if (env == null) return;

        if (tempText != null)
            tempText.text = env.Temperature.ToString("F0");

        if (humText != null)
            humText.text = env.Humidity.ToString("F0");

        if (presText != null)
            presText.text = env.Pressure.ToString("F0");
    }
}
