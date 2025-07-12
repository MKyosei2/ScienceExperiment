using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class SelectionSummaryDisplay : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public TextMeshProUGUI outputText;

    private void Update()
    {
        if (outputText != null)
        {
            outputText.text = "@InferenceBot\n" +
                              "element: " + holder.selectedElementID + "\n" +
                              "tool: " + holder.selectedToolID + "\n" +
                              "condition: " + holder.selectedConditionID;
        }
    }
}