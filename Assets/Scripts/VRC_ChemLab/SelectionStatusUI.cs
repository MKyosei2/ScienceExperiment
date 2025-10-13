// Assets/Scripts/U#/SelectionStatusUI.cs
using UdonSharp;
using UnityEngine;
using TMPro;

/// 任意。別UIに選択状況を出したい場合に使う（更新は外部から呼ぶ）
public class SelectionStatusUI : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public TextMeshProUGUI text;

    public void Refresh()
    {
        if (selected == null || text == null) return;
        text.text = $"Elements: {selected.GetElementCount()} / Tools: {selected.GetToolCount()} / Condition: {(string.IsNullOrEmpty(selected.conditionID) && selected.conditionObject == null ? "None" : "OK")}";
    }
}
