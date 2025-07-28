using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender : UdonSharpBehaviour
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
        string msg = $"@BotRequest\nelement: {el}\ntool: {tl}\ncondition: {conditionID}";

        if (monitor) monitor.Log("📡 Discord送信: " + msg);
        if (statusText) statusText.text = "🧪 Discord Botに送信中…";

        // ここでDiscord送信APIを呼ぶ

        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    // Discord応答が来たらこれを呼ぶ
    public void OnDiscordResponse(string result)
    {
        if (responseReceived) return;
        responseReceived = true;
        if (statusText) statusText.text = "✅ Discord応答あり：" + result;
        if (monitor) monitor.Log("🧪 Discord応答：" + result);

        if (experimentPlayer != null) experimentPlayer.PlaySequence();
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;
        if (statusText) statusText.text = "⚠️ Discord Bot無応答。ローカル演出を実行します。";
        if (monitor) monitor.Log("⚠️ 5秒間応答なし→ローカル演出へ切り替え");

        if (experimentPlayer != null) experimentPlayer.PlaySequence();
    }
}
