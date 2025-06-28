using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;

public class AIRequestSender : UdonSharpBehaviour
{
    public CompoundQueryBuilder builder;
    public AICompoundResponseParser parser;
    public Text statusDisplay;

    public VRCUrl apiBase; // 例: https://ai.yourdomain.com/chem

    public void SendToAI(string[] elements, string conditionKey)
    {
        string query = builder.BuildQuery(elements, conditionKey);
        string fullUrl = $"{apiBase.Get()}?{query}";

        statusDisplay.text = "AIに送信中...";
        VRCStringDownloader.LoadUrl(new VRCUrl(fullUrl), this);
    }

    public void OnVRCStringDownloadComplete(VRCStringDownloader downloader, string response)
    {
        parser?.ParseResponse(response);
        statusDisplay.text = "完了";
    }

    public void OnVRCStringDownloadError(VRCStringDownloader downloader, string error)
    {
        statusDisplay.text = "AI応答エラー";
    }
}
