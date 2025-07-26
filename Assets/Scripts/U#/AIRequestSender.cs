using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender : UdonSharpBehaviour
{
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;
    public VisualExperimentPlayer experimentPlayer;
    public ExperimentController experimentController; // ← 明示的参照（安全）

    private bool responseReceived = false;

    public void SendToAI(string elementID, string toolID, string conditionID)
    {
        SendRequest(elementID, toolID, conditionID);
    }

    public void SendRequest(string elementID, string toolID, string conditionID)
    {
        string url = $"https://api.example.com/experiment?e={elementID}&t={toolID}&c={conditionID}";
        statusText.text = "🧪 実験データ送信中...";
        responseReceived = false;

        if (monitor != null) monitor.Log("Request sent to: " + url);

        // 応答待ちタイマー
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);

        // 模擬応答（2秒後）
        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 2.0f);
    }

    public void MockReceiveResponse()
    {
        if (responseReceived) return;
        responseReceived = true;

        statusText.text = "✅ 応答あり：酸素が発生しました！";
        if (monitor != null) monitor.Log("成功応答: 酸素発生");

        if (experimentPlayer != null) experimentPlayer.PlaySequence();

        // ✅ 通知（成功）
        if (experimentController != null)
        {
            experimentController.MarkResponseReceived();
        }
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;

        responseReceived = true;
        statusText.text = "⚠️ 応答がありません。ローカル演出を実行します。";
        if (monitor != null) monitor.Log("⚠️ 応答なし: Fallback 実行");

        // ✅ 通知（失敗）
        if (experimentController != null)
        {
            experimentController.FallbackIfNoResponse();
        }
    }
}
