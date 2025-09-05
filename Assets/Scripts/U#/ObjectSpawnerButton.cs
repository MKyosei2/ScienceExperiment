using UdonSharp;
using UnityEngine;

public class ObjectSpawnerButton : UdonSharpBehaviour
{
    [Header("Spawn/Select")]
    public GenericSelector selector;
    public SelectionActionController action;
    public SelectedObjectHolder selected;

    [Header("Optional IDs")]
    public string elementId;
    public string toolId;
    public string conditionId;

    public void SpawnElement()
    {
        if (selector != null) { selector.category = ESelectorCategory.Element; selector.SpawnOrReplace(); }
        else if (action != null) action.Execute();
        else if (selected != null && !string.IsNullOrEmpty(elementId)) selected.AddElement(elementId);
    }

    public void SpawnTool()
    {
        if (selector != null) { selector.category = ESelectorCategory.Tool; selector.SpawnOrReplace(); }
        else if (action != null) action.Execute();
        else if (selected != null && !string.IsNullOrEmpty(toolId)) selected.AddTool(toolId);
    }

    public void SetConditionById()
    {
        if (selected != null && !string.IsNullOrEmpty(conditionId)) selected.SetCondition(conditionId);
    }
}
