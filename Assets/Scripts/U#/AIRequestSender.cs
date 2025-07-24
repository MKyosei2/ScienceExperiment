using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender : UdonSharpBehaviour
{
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;
    public VisualExperimentPlayer experimentPlayer;

    private string latestElementID;
    private string latestToolID;
    private string latestConditionID;

    private bool responseReceived = false;

    public void SendToAI(string elementID, string toolID, string conditionID)
    {
        latestElementID = elementID;
        latestToolID = toolID;
        latestConditionID = conditionID;

        SendRequest(elementID, toolID, conditionID);
    }

    public void SendRequest(string elementID, string toolID, string conditionID)
    {
        string url = $"https://api.example.com/experiment?e={elementID}&t={toolID}&c={conditionID}";
        statusText.text = "🧪 実験を開始しました。";
        responseReceived = false;

        if (monitor != null) monitor.Log("Request sent to: " + url);

        // 通信の代替として2秒待ってレスポンスを模倣
        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 2.0f);

        // 5秒以内にレスポンスが来なければローカル演出
        SendCustomEventDelayedSeconds(nameof(FallbackIfNoResponse), 5.0f);
    }

    public void MockReceiveResponse()
    {
        if (responseReceived) return;
        responseReceived = true;

        string result = "🔥 化学反応成功！酸素が放出されました。";
        statusText.text = result;
        if (monitor != null) monitor.Log(result);

        if (experimentPlayer != null)
        {
            experimentPlayer.PlaySequence(); // 修正済み：引数なし
        }
    }

    public void FallbackIfNoResponse()
    {
        if (responseReceived) return;

        responseReceived = true;
        string result = "⚠️ 通信に失敗しました。ローカル演出を実行します。";
        statusText.text = result;
        if (monitor != null) monitor.Log(result);

        if (experimentPlayer != null)
        {
            experimentPlayer.PlaySequence(); // 修正済み：引数なし
        }
    }
}
