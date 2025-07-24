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

    [Header("演出フォールバック先")]
    public ExperimentController controller;

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

        // 5秒後フォールバック
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void MockReceiveResponse()
    {
        if (hasResponded) return;
        hasResponded = true;

        string result = "✅ 実験完了: 化学反応成功！";
        if (statusText != null) statusText.text = result;
        if (monitor != null) monitor.Log(result);

        if (controller != null) controller.ApplyVisualEffect();
    }

    public void FallbackIfNoResponse()
    {
        if (hasResponded) return;
        hasResponded = true;

        string result = "⚠️ 通信失敗。ローカル演出を実行します。";
        if (statusText != null) statusText.text = result;
        if (monitor != null) monitor.Log(result);

        if (controller != null) controller.ApplyVisualEffect();
    }
}
