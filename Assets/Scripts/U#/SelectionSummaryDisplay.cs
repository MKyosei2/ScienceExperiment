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
            string element = holder.selectedElementIDs.Length > 0 ? holder.selectedElementIDs[0] : "";
            string tool = holder.selectedToolIDs.Length > 0 ? holder.selectedToolIDs[0] : "";
            string condition = holder.selectedConditionID;

            outputText.text = "@InferenceBot\n" +
                              "element: " + element + "\n" +
                              "tool: " + tool + "\n" +
                              "condition: " + condition;
        }
    }
}