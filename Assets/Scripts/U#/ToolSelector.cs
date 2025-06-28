using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ToolSelector : UdonSharpBehaviour
{
    [Header("ツール情報（ScriptableObject から抽出済み）")]
    public string[] toolIDs;
    public string[] toolNames;

    [Header("UI")]
    public Text selectedText;
    public Button[] toolButtons;

    private int selectedIndex = -1;

    void Start()
    {
        for (int i = 0; i < toolButtons.Length && i < toolNames.Length; i++)
        {
            Text btnText = toolButtons[i].GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = toolNames[i];
            }
        }

        if (selectedText != null)
        {
            selectedText.text = "器具未選択";
        }
    }

    public void Select(int index)
    {
        selectedIndex = index;
        if (selectedText != null && index >= 0 && index < toolNames.Length)
        {
            selectedText.text = "選択中: " + toolNames[index];
        }
    }

    public string GetSelectedToolID()
    {
        return selectedIndex >= 0 && selectedIndex < toolIDs.Length
            ? toolIDs[selectedIndex]
            : "";
    }

    // 各ボタンにインスペクターから割り当てる
    public void OnClickTool0() => Select(0);
    public void OnClickTool1() => Select(1);
    public void OnClickTool2() => Select(2);
    public void OnClickTool3() => Select(3);
    public void OnClickTool4() => Select(4);
    public void OnClickTool5() => Select(5);
}