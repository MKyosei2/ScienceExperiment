using UdonSharp;
using UnityEngine;
using TMPro;

public class ModeSwitcher : UdonSharpBehaviour
{
    public TextMeshProUGUI modeLabel;
    public GameObject experimentButton;
    public GameObject pcUIRoot;
    public GameObject vrUIRoot;

    private bool isPCMode = true;

    public void ToggleMode()
    {
        isPCMode = !isPCMode;
        UpdateUI();
    }

    public bool IsPCMode() => isPCMode;

    private void UpdateUI()
    {
        if (modeLabel != null) modeLabel.text = isPCMode ? "🖱 PCモード" : "🎮 VRモード";
        if (pcUIRoot != null) pcUIRoot.SetActive(isPCMode);
        if (vrUIRoot != null) vrUIRoot.SetActive(!isPCMode);
        if (experimentButton != null) experimentButton.SetActive(isPCMode);
    }
}
