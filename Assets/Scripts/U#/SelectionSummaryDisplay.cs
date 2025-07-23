using UdonSharp;
using UnityEngine;
using TMPro;

public class SelectionSummaryDisplay : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public TextMeshProUGUI outputText;

    private void Update()
    {
        if (outputText == null || holder == null) return;

        string elements = FormatArray(holder.selectedElementIDs);
        string tools = FormatArray(holder.selectedToolIDs);
        string condition = string.IsNullOrEmpty(holder.selectedConditionID) ? "なし" : holder.selectedConditionID;

        outputText.text = "@InferenceBot\n" +
                          "element: " + elements + "\n" +
                          "tool: " + tools + "\n" +
                          "condition: " + condition;
    }

    private string FormatArray(string[] array)
    {
        if (array == null || array.Length == 0) return "なし";

        string result = "";
        for (int i = 0; i < array.Length; i++)
        {
            result += array[i];
            if (i < array.Length - 1) result += ", ";
        }
        return result;
    }
}
