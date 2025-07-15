using UdonSharp;
using UnityEngine;
using TMPro;

public class ModeSwitcher : UdonSharpBehaviour
{
    public bool isVRMode = true;

    public GameObject pcUIRoot;
    public GameObject vrUIRoot;
    public GameObject experimentButton;
    public TextMeshProUGUI modeLabel;

    public void ToggleMode()
    {
        isVRMode = !isVRMode;
        UpdateUI();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (pcUIRoot != null) pcUIRoot.SetActive(!isVRMode);
        if (vrUIRoot != null) vrUIRoot.SetActive(isVRMode);
        if (experimentButton != null) experimentButton.SetActive(!isVRMode);

        if (modeLabel != null)
        {
            modeLabel.text = isVRMode ? "🎮 VRモード" : "🖱 PCモード";
        }
    }

    public bool IsVRMode()
    {
        return isVRMode;
    }
}
