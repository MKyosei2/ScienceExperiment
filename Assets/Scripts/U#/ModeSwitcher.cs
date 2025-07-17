using UdonSharp;
using UnityEngine;
using TMPro;

public class ModeSwitcher : UdonSharpBehaviour
{
    public TextMeshProUGUI modeLabel;
    public GameObject experimentButton;
    public GameObject pcUIRoot;
    public GameObject vrUIRoot;

    public bool isPCMode = true;

    public void ToggleMode()
    {
        isPCMode = !isPCMode;
        UpdateUI();
    }

    public bool IsPCMode()
    {
        return isPCMode;
    }

    private void UpdateUI()
    {
        modeLabel.text = isPCMode ? "🖱 PCモード" : "🎮 VRモード";
        pcUIRoot.SetActive(isPCMode);
        vrUIRoot.SetActive(!isPCMode);
        experimentButton.SetActive(isPCMode);
    }
}
