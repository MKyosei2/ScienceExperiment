using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;

    public void RunExperiment()
    {
        if (holder == null || requestSender == null) return;

        string elementID = holder.selectedElementID;
        string toolID = holder.selectedToolID;
        string conditionID = holder.selectedConditionID;

        if (string.IsNullOrWhiteSpace(elementID) ||
            string.IsNullOrWhiteSpace(toolID) ||
            string.IsNullOrWhiteSpace(conditionID))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が未完了");
            return;
        }

        Debug.Log("🧪 実験開始: " + elementID + " x " + toolID + " x " + conditionID);
        requestSender.SendToAI(elementID, toolID, conditionID);
    }
}
