using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentSelector : UdonSharpBehaviour
{
    [SerializeField] private string[] elementSymbols;
    [SerializeField] private string[] toolIDs;
    [SerializeField] private string[] conditionIDs;

    public Text selectedElementText;
    public Text selectedToolText;
    public Text selectedConditionText;

    private int selectedElementIndex = -1;
    private int selectedToolIndex = -1;
    private int selectedConditionIndex = -1;

    // UI用（ボタンから）
    public void SelectElement(int index)
    {
        if (index >= 0 && index < elementSymbols.Length)
        {
            selectedElementIndex = index;
            if (selectedElementText != null)
                selectedElementText.text = "元素: " + elementSymbols[index];
        }
    }

    public void SelectTool(int index)
    {
        if (index >= 0 && index < toolIDs.Length)
        {
            selectedToolIndex = index;
            if (selectedToolText != null)
                selectedToolText.text = "器具: " + toolIDs[index];
        }
    }

    public void SelectCondition(int index)
    {
        if (index >= 0 && index < conditionIDs.Length)
        {
            selectedConditionIndex = index;
            if (selectedConditionText != null)
                selectedConditionText.text = "環境: " + conditionIDs[index];
        }
    }

    // 追加：3Dオブジェクトからの string 選択対応
    public void SelectElementBySymbol(string symbol)
    {
        for (int i = 0; i < elementSymbols.Length; i++)
        {
            if (elementSymbols[i] == symbol)
            {
                selectedElementIndex = i;
                if (selectedElementText != null)
                    selectedElementText.text = "元素: " + symbol;
                break;
            }
        }
    }

    public void SelectToolByID(string id)
    {
        for (int i = 0; i < toolIDs.Length; i++)
        {
            if (toolIDs[i] == id)
            {
                selectedToolIndex = i;
                if (selectedToolText != null)
                    selectedToolText.text = "器具: " + id;
                break;
            }
        }
    }

    public void SelectConditionByID(string id)
    {
        for (int i = 0; i < conditionIDs.Length; i++)
        {
            if (conditionIDs[i] == id)
            {
                selectedConditionIndex = i;
                if (selectedConditionText != null)
                    selectedConditionText.text = "環境: " + id;
                break;
            }
        }
    }

    public string GetSymbol() => (selectedElementIndex >= 0 && selectedElementIndex < elementSymbols.Length) ? elementSymbols[selectedElementIndex] : "";
    public string GetToolID() => (selectedToolIndex >= 0 && selectedToolIndex < toolIDs.Length) ? toolIDs[selectedToolIndex] : "";
    public string GetConditionID() => (selectedConditionIndex >= 0 && selectedConditionIndex < conditionIDs.Length) ? conditionIDs[selectedConditionIndex] : "";
}
