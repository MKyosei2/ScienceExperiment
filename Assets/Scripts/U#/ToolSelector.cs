using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ToolSelector : UdonSharpBehaviour
{
    public ToolData[] availableTools;
    public Text selectedText;
    public Button[] toolButtons;

    private int selectedIndex = -1;

    void Start()
    {
        for (int i = 0; i < toolButtons.Length && i < availableTools.Length; i++)
        {
            var btnText = toolButtons[i].GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = availableTools[i].toolName;

            int index = i;
            toolButtons[i].onClick.AddListener(() => Select(index));
        }

        if (selectedText != null)
            selectedText.text = "器具未選択";
    }

    public void Select(int index)
    {
        selectedIndex = index;
        if (selectedText != null && index >= 0 && index < availableTools.Length)
        {
            selectedText.text = "選択中: " + availableTools[index].toolName;
        }
    }

    public string GetSelectedToolID()
    {
        return selectedIndex >= 0 && selectedIndex < availableTools.Length
            ? availableTools[selectedIndex].toolID
            : "";
    }
}