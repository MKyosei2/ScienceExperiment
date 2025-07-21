using UdonSharp;
using UnityEngine;
using TMPro;

public class StatusTextUI : UdonSharpBehaviour
{
    public TextMeshProUGUI statusText;

    public void Show(string message)
    {
        statusText.text = message;
    }
}