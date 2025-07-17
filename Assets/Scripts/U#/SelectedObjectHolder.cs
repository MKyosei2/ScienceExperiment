using UdonSharp;
using UnityEngine;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    public string[] selectedElementIDs = new string[8];
    public string[] selectedToolIDs = new string[8];
    public string selectedConditionID = "";

    private int elementCount = 0;
    private int toolCount = 0;

    public void AddElement(string id)
    {
        if (elementCount < selectedElementIDs.Length)
            selectedElementIDs[elementCount++] = id;
    }

    public void AddTool(string id)
    {
        if (toolCount < selectedToolIDs.Length)
            selectedToolIDs[toolCount++] = id;
    }

    public void SetCondition(string id)
    {
        selectedConditionID = id;
    }

    public void ClearAll()
    {
        for (int i = 0; i < selectedElementIDs.Length; i++) selectedElementIDs[i] = "";
        for (int i = 0; i < selectedToolIDs.Length; i++) selectedToolIDs[i] = "";
        selectedConditionID = "";
        elementCount = 0;
        toolCount = 0;
    }
}