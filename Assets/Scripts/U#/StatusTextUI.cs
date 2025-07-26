using UdonSharp;
using UnityEngine;
using TMPro;

public class StatusTextUI : UdonSharpBehaviour
{
    [Header("表示対象UI")]
    public TextMeshProUGUI statusText;

    [Header("参照する選択情報")]
    public SelectedObjectHolder holder;

    // ✅ 外部から直接呼び出してメッセージ表示
    public void Show(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        else
        {
            Debug.LogWarning("⚠️ StatusTextUI: statusText が未設定です");
        }
    }

    // ✅ 選択状況を表示（Element / Tool / Condition）
    public void ShowCurrentSelection()
    {
        if (statusText == null || holder == null)
        {
            Debug.LogWarning("⚠️ StatusTextUI: statusText または holder が未設定です");
            return;
        }

        string elements = FormatArray(holder.selectedElementIDs);
        string tools = FormatArray(holder.selectedToolIDs);
        string condition = string.IsNullOrEmpty(holder.selectedConditionID) ? "なし" : holder.selectedConditionID;

        string result = $"🧪 元素: {elements}\n🔧 器具: {tools}\n🌡️ 条件: {condition}";
        statusText.text = result;
    }

    // 🔧 配列整形
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
