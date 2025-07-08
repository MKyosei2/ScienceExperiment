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

    public string GetSymbol() => (selectedElementIndex >= 0 && selectedElementIndex < elementSymbols.Length) ? elementSymbols[selectedElementIndex] : "";
    public string GetToolID() => (selectedToolIndex >= 0 && selectedToolIndex < toolIDs.Length) ? toolIDs[selectedToolIndex] : "";
    public string GetConditionID() => (selectedConditionIndex >= 0 && selectedConditionIndex < conditionIDs.Length) ? conditionIDs[selectedConditionIndex] : "";
}