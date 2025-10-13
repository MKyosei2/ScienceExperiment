using UdonSharp;
using UnityEngine;
using TMPro;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    [Header("Zones")]
    public Transform elementZone, toolZone, conditionZone;

    [Header("UI")]
    public TextMeshProUGUI statusText;

    private const int MaxElements = 8, MaxTools = 8;

    public GameObject[] elementObjects = new GameObject[MaxElements];
    public GameObject[] toolObjects = new GameObject[MaxTools];
    public GameObject conditionObject;

    public string[] elementIDs = new string[MaxElements];
    public string[] toolIDs = new string[MaxTools];
    public string conditionID = "";

    private int elementCount = 0, toolCount = 0;

    public bool AddSelection(SelectionCategory category, GameObject obj, string idOrName = "")
    {
        if (obj == null) return false;
        string id = string.IsNullOrEmpty(idOrName) ? obj.name : idOrName;

        if (category == SelectionCategory.Element)
        { if (elementCount >= MaxElements) return false; elementObjects[elementCount] = obj; elementIDs[elementCount] = id; elementCount++; RefreshUI(); return true; }
        else if (category == SelectionCategory.Tool)
        { if (toolCount >= MaxTools) return false; toolObjects[toolCount] = obj; toolIDs[toolCount] = id; toolCount++; RefreshUI(); return true; }
        else
        { conditionObject = obj; conditionID = id; RefreshUI(); return true; }
    }

    public void SetAny(GameObject go)
    {
        if (go == null) return;
        if (elementZone != null && go.transform.IsChildOf(elementZone)) { AddSelection(SelectionCategory.Element, go, go.name); return; }
        if (toolZone != null && go.transform.IsChildOf(toolZone)) { AddSelection(SelectionCategory.Tool, go, go.name); return; }
        if (conditionZone != null && go.transform.IsChildOf(conditionZone)) { AddSelection(SelectionCategory.Condition, go, go.name); return; }
        if (elementCount < 2) { AddSelection(SelectionCategory.Element, go, go.name); return; }
        if (toolCount < 1) { AddSelection(SelectionCategory.Tool, go, go.name); return; }
        AddSelection(SelectionCategory.Condition, go, go.name);
    }

    public void ClearCondition() { conditionObject = null; conditionID = ""; RefreshUI(); }
    public void ClearAll()
    {
        for (int i = 0; i < MaxElements; i++) { elementObjects[i] = null; elementIDs[i] = ""; }
        for (int i = 0; i < MaxTools; i++) { toolObjects[i] = null; toolIDs[i] = ""; }
        elementCount = 0; toolCount = 0; conditionObject = null; conditionID = ""; RefreshUI();
    }

    public bool IsValid() { return elementCount >= 2 && toolCount >= 1 && (!string.IsNullOrEmpty(conditionID) || conditionObject != null); }

    private void RefreshUI()
    {
        if (statusText == null) return;
        statusText.text = $"Elements:{elementCount}  Tools:{toolCount}  Condition:{(string.IsNullOrEmpty(conditionID) && conditionObject == null ? "None" : "OK")}";
    }

    public int GetElementCount() { return elementCount; }
    public int GetToolCount() { return toolCount; }
}
