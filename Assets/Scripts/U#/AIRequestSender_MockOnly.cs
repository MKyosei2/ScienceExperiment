using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender_MockOnly : UdonSharpBehaviour
{
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;
    public VisualExperimentPlayer experimentPlayer;
    public ExperimentController experimentController;

    private bool responseReceived = false;

    public void SendToAI(string elementID, string toolID, string conditionID)
    {
        SendRequest(elementID, toolID, conditionID);
    }

    public void SendRequest(string elementID, string toolID, string conditionID)
    {
        statusText.text = "🧪 モック送信中（Discord通信なし）";
        responseReceived = false;

        string message = $"@InferenceBot\n" +
                         $"element: {elementID}\n" +
                         $"tool: {toolID}\n" +
                         $"condition: {conditionID}";
        if (monitor != null) monitor.Log("📡 モック送信: " + message);

        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 1.0f);
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void MockReceiveResponse()
    {
        Debug.Log("⭐ MockReceiveResponse実行");
        if (responseReceived) return;
        responseReceived = true;

        statusText.text = "✅ モック応答あり：酸素が発生しました！";
        if (monitor != null)
        {
            monitor.Log("🧪 モック応答：酸素生成演出再生");
            monitor.Log("▶️ 演出シーケンス開始");
        }

        if (experimentPlayer != null)
        {
            Debug.Log("🎬 experimentPlayer.PlaySequence() 実行");
            experimentPlayer.PlaySequence();
        }
        else
        {
            Debug.Log("❌ experimentPlayerがnullです");
        }

        if (experimentController != null)
        {
            experimentController.MarkResponseReceived();
        }
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;

        statusText.text = "⚠️ 応答なし。ローカル演出を実行します。";
        if (monitor != null) monitor.Log("⚠️ 応答なし：フォールバック演出を再生");

        if (experimentController != null)
        {
            experimentController.FallbackIfNoResponse();
        }
    }
}
