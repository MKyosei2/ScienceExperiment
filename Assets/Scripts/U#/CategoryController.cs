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

    [Header("Selector (optional)")]
    public GenericSelector selectorToSet;
    public int selectorEnumValue = 0; // 0:Element 1:Tool 2:Condition

    private int currentIndex = -1;

    public void SetCategoryByName(string name)
    {
        if (name == "Element") ApplySimple(0);
        else if (name == "Tool") ApplySimple(1);
        else if (name == "Condition") ApplySimple(2);
        else Debug.LogWarning("[CategoryController] Category not found: " + name);
    }

    public void SetCategoryByIndex(int i)
    {
        ApplySimple(i);
    }

    private void ApplySimple(int i)
    {
        currentIndex = i;

        ToggleArray(elementShow, i == 0);
        ToggleArray(toolShow, i == 1);
        ToggleArray(conditionShow, i == 2);

        ToggleArray(elementHide, i != 0);
        ToggleArray(toolHide, i != 1);
        ToggleArray(conditionHide, i != 2);

        if (selectorToSet != null)
        {
            selectorToSet.category = (ESelectorCategory)selectorEnumValue;
        }
    }

    private void ToggleArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        for (int k = 0; k < arr.Length; k++)
            if (arr[k]) arr[k].SetActive(state);
    }
}
