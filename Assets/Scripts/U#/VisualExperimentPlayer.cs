using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class VisualExperimentPlayer : UdonSharpBehaviour
{
    public Text output; // 任意

    public void PlayStart(SelectedObjectHolder sel)
    {
        if (output) output.text = "実験開始…";
        Debug.Log("[Visual] Start");
    }

    public void PlayMessage(string message)
    {
        if (output) output.text = message;
        Debug.Log("[Visual] " + message);
    }

    public void PlayFallback()
    {
        if (output) output.text = "（フォールバック演出）";
        Debug.Log("[Visual] Fallback");
    }
}
