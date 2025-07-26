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

    [Header("演出プレイヤー")]
    public VisualExperimentPlayer experimentPlayer;

    [Header("ステータスUI")]
    public UdonBehaviour statusTextUI;

    private bool responseReceived = false;

    public void StartExperiment()
    {
        Debug.Log("🧪 StartExperiment() 呼び出し");

        if (Utilities.IsValid(Networking.LocalPlayer) && Networking.LocalPlayer.IsUserInVR())
        {
            Debug.Log("🎮 VRモードでは Trigger 実行に任せます");
            return;
        }

        // 遅延して反応（Prefab反映待ち）
        SendCustomEventDelayedFrames(nameof(RunExperimentIfValid), 1);
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

    public void RunExperimentIfValid()
    {
        Debug.Log("📦 RunExperimentIfValid() 呼び出し確認");

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

        string elementID = string.Join(",", elements);
        string toolID = string.Join(",", tools);
        string conditionID = condition;

        Debug.Log("🧪 実験データ送信: " + elementID + " / " + toolID + " / " + conditionID);
        responseReceived = false;

        requestSender.SendToAI(elementID, toolID, conditionID);

        // 5秒後に応答がなければフォールバック
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);

        // リアルタイムシェーダー効果（応急演出）
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

        // 通常演出の構築（成功パターン）
        if (experimentPlayer != null)
        {
            string elementCloneName = elements[0] + "(Clone)";
            GameObject target = GameObject.Find(elementCloneName);
            if (target == null)
            {
                Debug.LogWarning($"⚠️ 実験オブジェクト {elementCloneName} が Scene に存在しません");
                return;
            }

            experimentPlayer.stepTypes = new StepType[]
            {
                StepType.EmissionChange,
                StepType.MoveElement,
                StepType.ShaderEffect
            };

            experimentPlayer.stepTargets = new GameObject[]
            {
                target, target, target
            };

            experimentPlayer.stepDurations = new float[]
            {
                0.6f, 0.8f, 1.0f
            };

            experimentPlayer.emissionColors = new Color[]
            {
                Color.green
            };

            experimentPlayer.moveOffsets = new Vector3[]
            {
                Vector3.up * 0.1f
            };

            experimentPlayer.shaderProperties = new string[]
            {
                "_BubbleSpeed"
            };

            experimentPlayer.shaderValues = new float[]
            {
                3.5f
            };

            experimentPlayer.stepSounds = new AudioClip[] { };

            experimentPlayer.PlaySequence();
            Debug.Log("🎬 実験シーケンスを構成して再生しました");
        }
    }

    public void MarkResponseReceived()
    {
        responseReceived = true;
        Debug.Log("✅ Response received flag set");
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;

        Debug.LogWarning("⚠️ 応答なし → ローカルフォールバック演出を実行");

        if (statusTextUI != null)
        {
            statusTextUI.SetProgramVariable("statusText", "⚠️ 応答なし。ローカル演出を再生します。");
            statusTextUI.SendCustomEvent("ShowStatus");
        }

        if (experimentPlayer != null && holder.selectedElementIDs.Length > 0)
        {
            string elementCloneName = holder.selectedElementIDs[0] + "(Clone)";
            GameObject target = GameObject.Find(elementCloneName);
            if (target == null) return;

            experimentPlayer.stepTypes = new StepType[]
            {
                StepType.EmissionChange,
                StepType.MoveElement
            };

            experimentPlayer.stepTargets = new GameObject[]
            {
                target, target
            };

            experimentPlayer.stepDurations = new float[]
            {
                0.5f, 0.6f
            };

            experimentPlayer.emissionColors = new Color[]
            {
                Color.red
            };

            experimentPlayer.moveOffsets = new Vector3[]
            {
                Vector3.down * 0.1f
            };

            experimentPlayer.PlaySequence();
            Debug.Log("🧪 Fallback演出（赤色警告）を再生しました");
        }
    }
}
