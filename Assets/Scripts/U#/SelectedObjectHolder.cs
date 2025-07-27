using UdonSharp;
using UnityEngine;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    public string selectedElementID;
    public string selectedToolID;
    public string selectedConditionID;

    public string[] selectedElementIDs = new string[0];
    public string[] selectedToolIDs = new string[0];

    public StatusTextUI statusTextUI;

    public void AddElement(string id)
    {
        selectedElementID = id;
        if (!Contains(selectedElementIDs, id))
        {
            selectedElementIDs = Append(selectedElementIDs, id);
        }
        UpdateUI();
    }

    public void AddTool(string id)
    {
        selectedToolID = id;
        if (!Contains(selectedToolIDs, id))
        {
            selectedToolIDs = Append(selectedToolIDs, id);
        }
        UpdateUI();
    }

    public void SetCondition(string id)
    {
        selectedConditionID = id;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (statusTextUI != null)
        {
            statusTextUI.ShowCurrentSelection();
        }
    }

    private string[] Append(string[] array, string newValue)
    {
        int len = array.Length;
        string[] newArray = new string[len + 1];
        for (int i = 0; i < len; i++) newArray[i] = array[i];
        newArray[len] = newValue;
        return newArray;
    }

    private bool Contains(string[] array, string target)
    {
        if (array == null) return false;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == target) return true;
        }
        return false;
    }
}
