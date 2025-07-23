using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender : UdonSharpBehaviour
{
    [Header("ステータス表示")]
    public TextMeshProUGUI statusText;

    [Header("反応エフェクト")]
    public Renderer reactionRenderer;

    [Header("ログ出力")]
    public VRExperimentMonitor monitor;

    private bool hasResponded = false;

    public void SendToAI(string elementID, string toolID, string conditionID)
    {
        hasResponded = false;

        if (statusText != null)
            statusText.text = "🧪 Status: 実験を開始しました。";

        string url = $"https://api.example.com/experiment?e={elementID}&t={toolID}&c={conditionID}";
        Debug.Log("🌐 送信先: " + url);
        if (monitor != null) monitor.Log("送信先: " + url);

        // 疑似応答を2秒後に発生
        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 2.0f);
        // 5秒経過しても応答がなければフォールバック
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void MockReceiveResponse()
    {
        if (hasResponded) return;
        hasResponded = true;

        string result = "✅ 実験完了: 化学反応成功！";
        if (statusText != null) statusText.text = result;
        if (monitor != null) monitor.Log(result);

        ApplyVisualEffect();
    }

    public void FallbackIfNoResponse()
    {
        if (hasResponded) return;
        hasResponded = true;

        string result = "⚠️ 通信失敗。ローカル演出を実行します。";
        if (statusText != null) statusText.text = result;
        if (monitor != null) monitor.Log(result);

        ApplyVisualEffect(); // 同じ演出でカバー
    }

    private void ApplyVisualEffect()
    {
        if (reactionRenderer == null)
        {
            Debug.LogWarning("⚠️ reactionRenderer が未設定");
            return;
        }

        Material mat = reactionRenderer.material;
        if (mat == null)
        {
            Debug.LogWarning("⚠️ マテリアルが未設定");
            return;
        }

        mat.SetFloat("_BubbleSpeed", 2.5f);
        mat.SetFloat("_WobbleAmount", 0.12f);
        mat.SetFloat("_HeatDistortion", 0.15f);
        mat.SetColor("_MainColor", new Color(0.2f, 0.6f, 1.0f, 1.0f));
    }
}
