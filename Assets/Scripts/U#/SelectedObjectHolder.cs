using UdonSharp;
using UnityEngine;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    public string[] selectedElementIDs = new string[0];
    public string[] selectedToolIDs = new string[0];
    public string selectedConditionID;
    public StatusTextUI statusTextUI; // 任意でUI更新

    public void AddElement(string id)
    {
        selectedElementIDs = Append(selectedElementIDs, id);
        UpdateUI();
    }
    public void AddTool(string id)
    {
        selectedToolIDs = Append(selectedToolIDs, id);
        UpdateUI();
    }
    public void SetCondition(string id)
    {
        selectedConditionID = id;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (statusTextUI != null) statusTextUI.ShowCurrentSelection();
    }

    private string[] Append(string[] array, string newValue)
    {
        int len = (array == null) ? 0 : array.Length;
        string[] newArray = new string[len + 1];
        for (int i = 0; i < len; i++) newArray[i] = array[i];
        newArray[len] = newValue;
        return newArray;
    }
}
