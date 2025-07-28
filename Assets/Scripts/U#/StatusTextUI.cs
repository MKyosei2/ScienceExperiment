using UdonSharp;
using UnityEngine;
using TMPro;

public class StatusTextUI : UdonSharpBehaviour
{
    public TextMeshProUGUI statusText;
    public SelectedObjectHolder holder;

    public void Show(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
    public void ShowCurrentSelection()
    {
        if (statusText == null || holder == null) return;
        string elements = FormatArray(holder.selectedElementIDs);
        string tools = FormatArray(holder.selectedToolIDs);
        string condition = string.IsNullOrEmpty(holder.selectedConditionID) ? "なし" : holder.selectedConditionID;
        statusText.text = $"🧪 元素: {elements}\n🔧 器具: {tools}\n🌡️ 条件: {condition}";
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
