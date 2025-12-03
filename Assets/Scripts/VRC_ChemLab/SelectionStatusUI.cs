using UdonSharp;
using UnityEngine;
using TMPro;

public class SelectionStatusUI : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public TextMeshProUGUI textUI;

    private void Update()
    {
        if (holder == null || textUI == null) return;

        string condition = string.IsNullOrEmpty(holder.conditionID)
            ? "None"
            : holder.conditionID;

        textUI.text =
            $"Elements: {holder.GetElementCount()}   " +
            $"Tools: {holder.GetToolCount()}   " +
            $"Condition: {condition}";
    }
}
