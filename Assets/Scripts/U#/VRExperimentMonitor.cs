using UdonSharp;
using UnityEngine;
using TMPro;

public class VRExperimentMonitor : UdonSharpBehaviour
{
    public TextMeshProUGUI logText;
    public void Log(string message)
    {
        if (logText) logText.text = message;
        Debug.Log(message);
    }
}
