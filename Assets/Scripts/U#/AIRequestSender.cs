using UdonSharp;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AIRequestSender : UdonSharpBehaviour
{
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;

    public void SendToAI(string elementID, string toolID, string conditionID)
    {
        SendRequest(elementID, toolID, conditionID);
    }

    public void SendRequest(string elementID, string toolID, string conditionID)
    {
        string url = $"https://api.example.com/experiment?e={elementID}&t={toolID}&c={conditionID}";
        statusText.text = "🧪 リクエスト送信中...";
        if (monitor != null) monitor.Log("Request sent to: " + url);

        // 疑似レスポンス
        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 2.0f);
    }

    public void MockReceiveResponse()
    {
        string result = "🔥 化学反応成功！酸素が放出されました。";
        statusText.text = result;
        if (monitor != null) monitor.Log(result);
    }
}
