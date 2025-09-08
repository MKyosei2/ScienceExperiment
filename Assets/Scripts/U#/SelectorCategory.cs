using UdonSharp;
using UnityEngine;
using TMPro;

public enum SelectionCategory { Element = 0, Tool = 1, Condition = 2 }

[AddComponentMenu("VRC Lab/SelectorCategory")]
public class SelectorCategory : UdonSharpBehaviour
{
    [Header("State")]
    public SelectionCategory current = SelectionCategory.Element;

    [Header("Propagate")]
    public SpawnSelectorButton[] spawnButtons;
    public SelectorObject[] selectorObjects;

    [Header("Optional UI show/hide")]
    public GameObject[] elementShow, elementHide;
    public GameObject[] toolShow, toolHide;
    public GameObject[] conditionShow, conditionHide;

    [Header("Optional Label")]
    public TextMeshProUGUI label;

    [Header("Listeners (SendCustomEvent \"OnCategoryChanged\")")]
    public UdonSharpBehaviour[] listeners;

    private void OnEnable() { Apply(); }

    public void SetElement() { current = SelectionCategory.Element; Apply(); }
    public void SetTool() { current = SelectionCategory.Tool; Apply(); }
    public void SetCondition() { current = SelectionCategory.Condition; Apply(); }
    public void SetByInt(int v) { if (v < 0) v = 0; if (v > 2) v = 2; current = (SelectionCategory)v; Apply(); }
    public void Next() { int v = (int)current + 1; if (v > 2) v = 0; current = (SelectionCategory)v; Apply(); }
    public void Prev() { int v = (int)current - 1; if (v < 0) v = 2; current = (SelectionCategory)v; Apply(); }

    private void Apply()
    {
        if (spawnButtons != null) for (int i = 0; i < spawnButtons.Length; i++) if (spawnButtons[i] != null) spawnButtons[i].category = current;
        if (selectorObjects != null) for (int i = 0; i < selectorObjects.Length; i++) if (selectorObjects[i] != null) selectorObjects[i].category = current;

        Toggle(elementShow, current == SelectionCategory.Element); Toggle(elementHide, current != SelectionCategory.Element);
        Toggle(toolShow, current == SelectionCategory.Tool); Toggle(toolHide, current != SelectionCategory.Tool);
        Toggle(conditionShow, current == SelectionCategory.Condition); Toggle(conditionHide, current != SelectionCategory.Condition);

        if (label != null) label.text = current == SelectionCategory.Element ? "Element" : current == SelectionCategory.Tool ? "Tool" : "Condition";
        if (listeners != null) for (int i = 0; i < listeners.Length; i++) if (listeners[i] != null) listeners[i].SendCustomEvent("OnCategoryChanged");
    }

    private void Toggle(GameObject[] arr, bool v) { if (arr == null) return; for (int i = 0; i < arr.Length; i++) if (arr[i] != null) arr[i].SetActive(v); }
}
