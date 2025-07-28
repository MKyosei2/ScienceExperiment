using UdonSharp;
using UnityEngine;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    public string[] selectedElementIDs = new string[0]; // 複数回押し対応
    public string[] selectedToolIDs = new string[0];
    public string selectedConditionID;
    public StatusTextUI statusTextUI;

    public void AddElement(string id)
    {
        selectedElementIDs = Append(selectedElementIDs, id); // 何度でも追加OK
        UpdateUI();
    }
    public void AddTool(string id)
    {
        selectedToolIDs = Append(selectedToolIDs, id); // 何度でも追加OK
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
        int len = array.Length;
        string[] newArray = new string[len + 1];
        for (int i = 0; i < len; i++) newArray[i] = array[i];
        newArray[len] = newValue;
        return newArray;
    }
}
