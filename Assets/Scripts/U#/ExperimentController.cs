using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentController : UdonSharpBehaviour
{
    [Header("必要な参照")]
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;

    [Header("反応対象のRenderer")]
    public Renderer reactionRenderer;

    [Header("VR Trigger 実行を許可")]
    public bool enableTriggerReaction = true;

    public void StartExperiment()
    {
        Debug.Log("🧪 StartExperiment() が呼ばれました");

        if (Utilities.IsValid(Networking.LocalPlayer) && Networking.LocalPlayer.IsUserInVR())
        {
            Debug.Log("🎮 VRモード中：ボタン操作は無効（Triggerを使用）");
            return;
        }

        RunExperimentIfValid();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerReaction) return;

        GameObject obj = other.gameObject;
        string objName = obj.name.ToLower();

        // VRプレイヤーの手などのオブジェクトに "hand" が含まれていると仮定
        if (objName.Contains("hand") || objName.Contains("controller") || objName.Contains("player"))
        {
            Debug.Log("🚶 VR Triggerゾーンに侵入 → 実験条件チェック");
            RunExperimentIfValid();
        }
    }

    private void RunExperimentIfValid()
    {
        if (holder == null || requestSender == null)
        {
            Debug.LogError("❌ holder または requestSender が未設定です");
            return;
        }

        string[] elementIDs = holder.selectedElementIDs;
        string[] toolIDs = holder.selectedToolIDs;
        string conditionID = holder.selectedConditionID;

        if (elementIDs.Length == 0 || toolIDs.Length == 0 || string.IsNullOrWhiteSpace(conditionID))
        {
            Debug.LogWarning("⚠️ 実験条件が未選択です");
            return;
        }

        string elementStr = string.Join(",", elementIDs);
        string toolStr = string.Join(",", toolIDs);

        Debug.Log($"✅ 実験条件: Element({elementStr}), Tool({toolStr}), Condition({conditionID})");

        // 🌊 Shader演出
        if (reactionRenderer != null)
        {
            Material mat = reactionRenderer.material;
            if (mat != null)
            {
                mat.SetFloat("_WobbleAmount", 0.12f);
                mat.SetFloat("_BubbleSpeed", 2.0f);
                mat.SetFloat("_HeatDistortion", 0.2f);
                mat.SetColor("_MainColor", new Color(0.2f, 0.6f, 1f, 1f));
                Debug.Log("🎨 Shader演出を適用しました");
            }
        }

        // 🔁 AI送信
        requestSender.SendToAI(elementStr, toolStr, conditionID);
    }
}
