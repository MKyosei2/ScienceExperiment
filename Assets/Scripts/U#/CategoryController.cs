using UdonSharp;
using UnityEngine;

public class CategoryController : UdonSharpBehaviour
{
    [Header("Simple 3-category wiring (optional)")]
    public GameObject[] elementShow;
    public GameObject[] elementHide;
    public GameObject[] toolShow;
    public GameObject[] toolHide;
    public GameObject[] conditionShow;
    public GameObject[] conditionHide;

    [Header("Selector Button (optional)")]
    public SpawnSelectorButton selectorToSet;  // GenericSelectorではなく SpawnSelectorButton
    public int selectorEnumValue = 0;          // 0:Element 1:Tool 2:Condition

    public void ShowElement()
    {
        ToggleArray(elementShow, true);
        ToggleArray(elementHide, false);
        ToggleArray(toolShow, false);
        ToggleArray(toolHide, true);
        ToggleArray(conditionShow, false);
        ToggleArray(conditionHide, true);

        if (selectorToSet != null) selectorToSet.category = SelectionCategory.Element;
    }

    public void ShowTool()
    {
        ToggleArray(elementShow, false);
        ToggleArray(elementHide, true);
        ToggleArray(toolShow, true);
        ToggleArray(toolHide, false);
        ToggleArray(conditionShow, false);
        ToggleArray(conditionHide, true);

        if (selectorToSet != null) selectorToSet.category = SelectionCategory.Tool;
    }

    public void ShowCondition()
    {
        ToggleArray(elementShow, false);
        ToggleArray(elementHide, true);
        ToggleArray(toolShow, false);
        ToggleArray(toolHide, true);
        ToggleArray(conditionShow, true);
        ToggleArray(conditionHide, false);

        if (selectorToSet != null) selectorToSet.category = SelectionCategory.Condition;
    }

    /// UI等から呼んで enum 値で設定したい場合
    public void ApplyEnumToSelector()
    {
        if (selectorToSet != null)
        {
            selectorToSet.category = (SelectionCategory)selectorEnumValue;
        }
    }

    private void ToggleArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        for (int k = 0; k < arr.Length; k++)
        {
            if (arr[k] != null) arr[k].SetActive(state);
        }
    }
}
