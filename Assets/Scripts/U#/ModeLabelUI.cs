using UdonSharp;
using UnityEngine;
using TMPro;

public class ModeLabelUI : UdonSharpBehaviour
{
    public TextMeshProUGUI modeText;

    public void SetMode(bool isVR)
    {
        modeText.text = isVR ? "Mode: VR" : "Mode: PC";
    }
}
