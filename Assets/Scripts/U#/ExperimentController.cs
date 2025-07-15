using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public AIRequestSender requestSender;
    public SelectedObjectHolder holder;

    public void RunExperiment()
    {
        if (requestSender == null || holder == null) return;

        string symbol = holder.selectedElementID;
        string toolID = holder.selectedToolID;
        string conditionID = holder.selectedConditionID;

        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(toolID) || string.IsNullOrWhiteSpace(conditionID))
        {
            Debug.Log("⚠️ 条件が未設定です");
            return;
        }

        int urlIndex = 0; // 条件による分岐はここに実装可
        requestSender.SendToAI(urlIndex);
    }
}
