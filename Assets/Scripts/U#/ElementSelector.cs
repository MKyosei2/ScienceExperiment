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

        if (selectedText != null)
        {
            selectedText.gameObject.SetActive(enable);
        }

        int count = loader.elementCount;
        for (int i = 0; i < elementButtons.Length && i < count; i++)
        {
            Text buttonText = elementButtons[i].GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = loader.symbols[i];
            }

            // 各ボタンに対応するメソッドをエディタで割り当ててください
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

    // 以下のようにインスペクターから呼べるメソッドを用意
    public void OnClickButton0() => Select(0);
    public void OnClickButton1() => Select(1);
    public void OnClickButton2() => Select(2);
    public void OnClickButton3() => Select(3);
    // 必要に応じて追加
}