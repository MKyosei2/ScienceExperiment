using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender aiRequestSender;
    public VRExperimentMonitor monitor;

    public void StartExperiment()
    {
        if (holder == null || aiRequestSender == null)
        {
            Debug.LogError("❌ holder または aiRequestSender が未設定");
            if (monitor) monitor.Log("❌ holder または aiRequestSender が未設定");
            return;
        }
        if (holder.selectedElementIDs == null || holder.selectedToolIDs == null || string.IsNullOrEmpty(holder.selectedConditionID))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が不足しています");
            if (monitor) monitor.Log("⚠️ 実験に必要な選択が不足しています");
            return;
        }
        // string[] を渡す。string ではない。必要なものだけ渡す
        aiRequestSender.SendToAI(holder.selectedElementIDs, holder.selectedToolIDs, holder.selectedConditionID);
    }
}
