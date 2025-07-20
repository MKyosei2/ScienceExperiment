using UdonSharp;
using UnityEngine;
using TMPro;

public class StatusTextUI : UdonSharpBehaviour
{
    public TextMeshProUGUI statusText;

    public void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }
}
