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

        string el = (elementIDs != null && elementIDs.Length > 0) ? string.Join("\",\"", elementIDs) : "未選択";
        string tl = (toolIDs != null && toolIDs.Length > 0) ? string.Join("\",\"", toolIDs) : "未選択";
        string cond = string.IsNullOrEmpty(conditionID) ? "未選択" : conditionID;

        string json = "{\"element\":[\"" + el + "\"],\"tool\":[\"" + tl + "\"],\"condition\":\"" + cond + "\"}";

        if (monitor != null) monitor.Log("📡 JSONログ出力: " + json);
        if (statusText != null) statusText.text = "🧪 JSONログを出力しました（外部が送信）";

        Debug.Log("[EXPERIMENT_REQUEST] " + json);

        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void OnDiscordResponse(string result)
    {
        if (responseReceived) return;
        responseReceived = true;

        if (statusText != null) statusText.text = "✅ Discord応答あり：" + result;
        if (monitor != null) monitor.Log("🧪 Discord応答：" + result);

        if (experimentPlayer != null)
        {
            experimentPlayer.PlaySequence();
        }
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;
        responseReceived = true;

        if (statusText != null) statusText.text = "⚠️ Discord Bot無応答。ローカル演出を実行します。";
        if (monitor != null) monitor.Log("⚠️ 5秒間応答なし → ローカル演出へ切り替え");

        if (experimentPlayer != null)
        {
            experimentPlayer.PlaySequence();
        }
    }
}
