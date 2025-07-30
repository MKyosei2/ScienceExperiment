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

        string el = (elementIDs != null && elementIDs.Length > 0) ? string.Join(",", elementIDs) : "未選択";
        string tl = (toolIDs != null && toolIDs.Length > 0) ? string.Join(",", toolIDs) : "未選択";
        string cond = string.IsNullOrEmpty(conditionID) ? "未選択" : conditionID;

        string msg = $"🧪 実験リクエスト\n元素: {el}\n器具: {tl}\n環境: {cond}";

        if (monitor) monitor.Log("📡 Discord送信: " + msg);
        if (statusText) statusText.text = "🧪 Discord Webhookに送信中…";

#if UNITY_EDITOR
        ExperimentRequestWriter.Write(msg);
#endif

        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void OnDiscordResponse(string result)
    {
        if (responseReceived) return;
        responseReceived = true;

        if (statusText) statusText.text = "✅ Discord応答あり：" + result;
        if (monitor) monitor.Log("🧪 Discord応答：" + result);

        if (experimentPlayer != null)
        {
            experimentPlayer.PlaySequence();
        }
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;

        if (statusText) statusText.text = "⚠️ Discord Bot無応答。ローカル演出を実行します。";
        if (monitor) monitor.Log("⚠️ 5秒間応答なし→ローカル演出へ切り替え");

        if (experimentPlayer != null)
        {
            experimentPlayer.PlaySequence();
        }
    }
}
