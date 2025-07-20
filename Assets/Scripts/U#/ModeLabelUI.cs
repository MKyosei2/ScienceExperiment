using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModeLabelUI : UdonSharpBehaviour
{
    public TextMeshProUGUI label;

    public void SetModeText(string mode)
    {
        if (label != null)
            label.text = $"Mode: {mode}";
    }
}
