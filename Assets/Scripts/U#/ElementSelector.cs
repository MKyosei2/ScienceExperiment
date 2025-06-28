using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ElementSelector : UdonSharpBehaviour
{
    public ElementLoader loader;
    public Text selectedText;
    public Button[] elementButtons;
    private int selectedIndex = -1;

    void Start()
    {
        string tag = "None";
        if (Networking.LocalPlayer != null)
        {
            tag = Networking.LocalPlayer.GetPlayerTag("CurrentRoom");
        }

        bool enable = tag == "ExperimentRoom";

        for (int i = 0; i < elementButtons.Length; i++)
        {
            elementButtons[i].gameObject.SetActive(enable);
        }
        if (selectedText != null) selectedText.gameObject.SetActive(enable);

        int count = loader.elementCount;
        for (int i = 0; i < elementButtons.Length && i < count; i++)
        {
            int ix = i;
            Text buttonText = elementButtons[i].GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = loader.symbols[i];
            }
            elementButtons[i].onClick.AddListener(() => Select(ix));
        }
    }

    public void Select(int index)
    {
        selectedIndex = index;
        if (selectedText != null)
        {
            selectedText.text = "選択中: " + loader.symbols[index];
        }
    }

    public string GetSelectedSymbol()
    {
        return selectedIndex >= 0 ? loader.symbols[selectedIndex] : "";
    }
}