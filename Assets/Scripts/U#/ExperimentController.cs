using UdonSharp;
using UnityEngine;
using TMPro;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;
    public VisualExperimentPlayer experimentPlayer;
    public Renderer reactionRenderer;
    public UdonSharpBehaviour statusTextUI;

    private bool responseReceived = false;

    public void StartExperiment()
    {
        Debug.Log("▶️ StartExperiment() 呼び出し");
        if (holder == null || requestSender == null)
        {
            Debug.LogError("❌ holder または requestSender が未設定");
            return;
        }

        if (holder.selectedElementIDs.Length == 0 || holder.selectedToolIDs.Length == 0 || string.IsNullOrEmpty(holder.selectedConditionID))
        {
            Debug.LogWarning("⚠️ 必須の選択が不足しています");
            return;
        }

        RunExperimentIfValid();
    }

    public void RunExperimentIfValid()
    {
        Debug.Log("🧪 RunExperimentIfValid() 呼び出し確認");

        string eID = string.Join("_", holder.selectedElementIDs);
        string tID = holder.selectedToolIDs[0];
        string cID = holder.selectedConditionID;

        Debug.Log($"🧪 実験データ送信: {eID} / {tID} / {cID}");

        // 安全構成で仮のターゲットを決定（Elementの最初）
        GameObject target = GameObject.Find(holder.selectedElementIDs[0] + "(Clone)");
        if (target == null)
        {
            Debug.LogError("❌ 対象ターゲットが見つかりません");
            return;
        }

        if (reactionRenderer == null)
        {
            Renderer r = target.GetComponent<Renderer>();
            if (r != null) reactionRenderer = r;
        }

        // stepTypes に合わせて配列を統一
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
            0.5f, 1.0f, 1.2f
        };

        experimentPlayer.emissionColors = new Color[]
        {
            Color.yellow, Color.black, Color.black
        };

        experimentPlayer.moveOffsets = new Vector3[]
        {
            Vector3.up * 0.1f, Vector3.zero, Vector3.zero
        };

        experimentPlayer.shaderProperties = new string[]
        {
            "_Shininess", "", ""
        };

        experimentPlayer.shaderValues = new float[]
        {
            0.8f, 0f, 0f
        };

        experimentPlayer.reactionRenderer = reactionRenderer;

        // 応答済みフラグをリセット
        responseReceived = false;

        // リクエスト送信（Mock or 本番）
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

        Debug.Log("⚠️ 応答がなかったため、フォールバック演出を再生します");

        if (experimentPlayer != null)
        {
            experimentPlayer.stepTypes = new StepType[]
            {
                StepType.MoveElement
            };

            GameObject fallbackTarget = GameObject.Find(holder.selectedElementIDs[0] + "(Clone)");
            experimentPlayer.stepTargets = new GameObject[] { fallbackTarget };
            experimentPlayer.stepDurations = new float[] { 1.0f };
            experimentPlayer.moveOffsets = new Vector3[] { Vector3.down * 0.2f };

            experimentPlayer.PlaySequence();
        }
    }
}
