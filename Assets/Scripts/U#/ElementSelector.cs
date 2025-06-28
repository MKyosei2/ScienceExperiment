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
        string tag = Networking.LocalPlayer?.GetPlayerTag("CurrentRoom");
        bool enable = tag == "ExperimentRoom";

        foreach (var btn in elementButtons) btn.gameObject.SetActive(enable);
        if (selectedText != null) selectedText.gameObject.SetActive(enable);

        for (int i = 0; i < elementButtons.Length && i < loader.elementCount; i++)
        {
            int ix = i;
            elementButtons[i].GetComponentInChildren<Text>().text = loader.symbols[i];
            elementButtons[i].onClick.AddListener(() => Select(ix));
        }
    }

    public void Select(int index)
    {
        selectedIndex = index;
        selectedText.text = $"選択中: {loader.symbols[index]}";
    }

    public string GetSelectedSymbol()
    {
        return selectedIndex >= 0 ? loader.symbols[selectedIndex] : "";
    }
}
