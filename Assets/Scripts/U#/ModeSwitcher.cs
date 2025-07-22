using UdonSharp;
using UnityEngine;
using TMPro;

public class ModeSwitcher : UdonSharpBehaviour
{
    public TextMeshProUGUI modeLabel;
    public GameObject experimentButton;

    private bool isPCMode = true;

    public void ToggleMode()
    {
        isPCMode = !isPCMode;
        UpdateUI();
    }

    public bool IsPCMode() => isPCMode;

    private void UpdateUI()
    {
        if (modeLabel != null) modeLabel.text = isPCMode ? "PC Mode" : "VR Mode";
        if (experimentButton != null) experimentButton.SetActive(isPCMode);
    }
}
