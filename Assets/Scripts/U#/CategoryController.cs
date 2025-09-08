using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/CategoryController")]
public class CategoryController : UdonSharpBehaviour
{
    public GameObject[] elementShow, elementHide;
    public GameObject[] toolShow, toolHide;
    public GameObject[] conditionShow, conditionHide;
    public SpawnSelectorButton selectorToSet;

    public void ShowElement() { Toggle(elementShow, true); Toggle(elementHide, false); Toggle(toolShow, false); Toggle(toolHide, true); Toggle(conditionShow, false); Toggle(conditionHide, true); if (selectorToSet != null) selectorToSet.category = SelectionCategory.Element; }
    public void ShowTool() { Toggle(elementShow, false); Toggle(elementHide, true); Toggle(toolShow, true); Toggle(toolHide, false); Toggle(conditionShow, false); Toggle(conditionHide, true); if (selectorToSet != null) selectorToSet.category = SelectionCategory.Tool; }
    public void ShowCondition() { Toggle(elementShow, false); Toggle(elementHide, true); Toggle(toolShow, false); Toggle(toolHide, true); Toggle(conditionShow, true); Toggle(conditionHide, false); if (selectorToSet != null) selectorToSet.category = SelectionCategory.Condition; }

    private void Toggle(GameObject[] arr, bool v) { if (arr == null) return; for (int i = 0; i < arr.Length; i++) if (arr[i] != null) arr[i].SetActive(v); }
}
