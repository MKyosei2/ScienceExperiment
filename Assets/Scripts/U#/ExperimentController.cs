using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender_MockOnly requestSender;
    public VisualExperimentPlayer experimentPlayer;
    public Renderer reactionRenderer;

    private bool responseReceived = false;

    public void StartExperiment()
    {
        Debug.Log("🚩 ExperimentController: StartExperiment 実行開始");

        if (holder == null || requestSender == null)
        {
            Debug.LogError("❌ holder または requestSender が未設定");
            return;
        }
        if (holder.selectedElementIDs.Length == 0 || holder.selectedToolIDs.Length == 0 || string.IsNullOrEmpty(holder.selectedConditionID))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が不足しています");
            return;
        }
        RunExperimentIfValid();
    }

    public void RunExperimentIfValid()
    {
        string eID = string.Join("_", holder.selectedElementIDs);
        string tID = holder.selectedToolIDs[0];
        string cID = holder.selectedConditionID;
        Debug.Log($"🧪 実験データ送信: {eID} / {tID} / {cID}");
        requestSender.SendToAI(eID, tID, cID);
    }

    public void MarkResponseReceived()
    {
        responseReceived = true;
    }
    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;
        Debug.Log("⚠️ 応答がなかったためローカル演出を実行します");
        experimentPlayer.PlaySequence();
    }
}
