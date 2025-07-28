using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender_MockOnly : UdonSharpBehaviour
{
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;
    public VisualExperimentPlayer experimentPlayer;

    private bool responseReceived = false;

    public void SendToAI(string[] elementIDs, string[] toolIDs, string conditionID)
    {
        responseReceived = false;

        string el = (elementIDs != null) ? string.Join(",", elementIDs) : "";
        string tl = (toolIDs != null) ? string.Join(",", toolIDs) : "";
        string msg = $"[MOCK] element: {el}\ntool: {tl}\ncondition: {conditionID}";

        if (monitor) monitor.Log("📡 モック送信: " + msg);
        if (statusText) statusText.text = "🧪 モック送信中…";

        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 1.0f);
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void MockReceiveResponse()
    {
        if (responseReceived) return;
        responseReceived = true;
        if (statusText) statusText.text = "✅ モック応答あり";
        if (monitor) monitor.Log("🧪 モック応答：演出再生");
        if (experimentPlayer != null) experimentPlayer.PlaySequence();
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;
        if (statusText) statusText.text = "⚠️ 応答なし。ローカル演出を実行します。";
        if (monitor) monitor.Log("⚠️ 応答なし：ローカル演出");
        if (experimentPlayer != null) experimentPlayer.PlaySequence();
    }
}
