using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentController : UdonSharpBehaviour
{
    [Header("選択情報")]
    public SelectedObjectHolder holder;

    [Header("通信・演出送信先")]
    public AIRequestSender requestSender;

    [Header("視覚演出対象")]
    public Renderer reactionRenderer;

    [Header("VRモード実行許可")]
    public bool enableTriggerReaction = true;

    public void StartExperiment()
    {
        Debug.Log("🧪 StartExperiment() 呼び出し");

        if (Utilities.IsValid(Networking.LocalPlayer) && Networking.LocalPlayer.IsUserInVR())
        {
            Debug.Log("🎮 VRモードでは Trigger 実行に任せます");
            return;
        }

        RunExperimentIfValid();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerReaction) return;

        GameObject obj = other.gameObject;
        string name = obj != null ? obj.name.ToLower() : "";

        if (name.Contains("hand") || name.Contains("controller") || name.Contains("player"))
        {
            Debug.Log("🖐 VR Trigger 検出 → 実験実行チェック");
            RunExperimentIfValid();
        }
    }

    private void RunExperimentIfValid()
    {
        if (holder == null || requestSender == null)
        {
            Debug.LogError("❌ holder または requestSender が未設定");
            return;
        }

        string[] elements = holder.selectedElementIDs;
        string[] tools = holder.selectedToolIDs;
        string condition = holder.selectedConditionID;

        if (elements.Length == 0 || tools.Length == 0 || string.IsNullOrWhiteSpace(condition))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が未完了");
            return;
        }

        // 代表1つずつ使って送信（ここは実際のID統合ロジックに応じて調整可能）
        string elementID = string.Join(",", elements);
        string toolID = string.Join(",", tools);
        string conditionID = condition;

        Debug.Log("🧪 実験データ送信: " + elementID + " / " + toolID + " / " + conditionID);
        requestSender.SendToAI(elementID, toolID, conditionID);

        // 演出も即時適用（通信失敗時の保険）
        if (reactionRenderer != null)
        {
            Material mat = reactionRenderer.material;
            if (mat != null)
            {
                mat.SetFloat("_BubbleSpeed", 2.0f);
                mat.SetFloat("_WobbleAmount", 0.12f);
                mat.SetFloat("_HeatDistortion", 0.2f);
                mat.SetColor("_MainColor", new Color(0.2f, 0.6f, 1f, 1f));
                Debug.Log("🎨 シェーダー演出適用");
            }
        }
    }
}
