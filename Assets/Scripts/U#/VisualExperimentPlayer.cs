using UdonSharp;
using UnityEngine;
using TMPro;

[AddComponentMenu("VRC Lab/VisualExperimentPlayer")]
public class VisualExperimentPlayer : UdonSharpBehaviour
{
    public TextMeshProUGUI output;
    public void Play(string text) { if (output != null) output.text = text; }
}
