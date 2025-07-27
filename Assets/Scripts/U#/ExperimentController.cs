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

        GameObject na = GameObject.Find("Na(Clone)");
        GameObject cl = GameObject.Find("Cl(Clone)");
        GameObject beaker = GameObject.Find("beaker(Clone)");

        if (na == null || cl == null || beaker == null)
        {
            Debug.LogError("❌ 実験に必要なオブジェクトのいずれかが見つかりません");
            return;
        }

        if (reactionRenderer == null)
        {
            reactionRenderer = beaker.GetComponent<Renderer>();
        }

        experimentPlayer.stepTypes = new StepType[]
        {
            StepType.MoveElement,
            StepType.MoveElement,
            StepType.ShaderEffect
        };
        experimentPlayer.stepTargets = new GameObject[] { na, cl, beaker };
        experimentPlayer.stepDurations = new float[] { 1.0f, 1.0f, 0.8f };
        experimentPlayer.emissionColors = new Color[] { Color.white, Color.white, Color.white };
        experimentPlayer.shaderProperties = new string[] { "", "", "_Shininess" };
        experimentPlayer.shaderValues = new float[] { 0f, 0f, 0.8f };
        experimentPlayer.moveOffsets = new Vector3[] { Vector3.down * 0.1f, Vector3.down * 0.1f, Vector3.zero };
        experimentPlayer.reactionRenderer = reactionRenderer;

        responseReceived = false;
        requestSender.SendToAI(eID, tID, cID);
    }

    public void MarkResponseReceived()
    {
        Debug.Log("✅ MarkResponseReceived 実行");
        responseReceived = true;
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;

        Debug.Log("⚠️ 応答がなかったためローカル演出を実行します");

        GameObject fallback = GameObject.Find(holder.selectedElementIDs[0] + "(Clone)");
        if (fallback == null) return;

        experimentPlayer.stepTypes = new StepType[] { StepType.MoveElement };
        experimentPlayer.stepTargets = new GameObject[] { fallback };
        experimentPlayer.stepDurations = new float[] { 1.0f };
        experimentPlayer.moveOffsets = new Vector3[] { Vector3.down * 0.2f };
        experimentPlayer.reactionRenderer = fallback.GetComponent<Renderer>();

        experimentPlayer.PlaySequence();
    }
}
