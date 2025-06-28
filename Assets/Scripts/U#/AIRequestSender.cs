using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class AIRequestSender : UdonSharpBehaviour
{
    public CompoundQueryBuilder builder;
    public AICompoundResponseParser parser;
    public Text statusDisplay;
    public VRCUrl apiBase;        // 例: https://ai.example.com/chem

    public void SendToAI(string[] elements, string conditionKey)
    {
        if (builder == null) return;

        string query = builder.BuildQuery(elements, conditionKey);
        string fullUrl = $"{apiBase.Get()}?{query}";

        statusDisplay.text = "AIに送信中…";
        VRCStringDownloader.LoadUrl(new VRCUrl(fullUrl), (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        parser?.ParseResponse(result.Result);
        statusDisplay.text = "完了";
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        statusDisplay.text = "AI応答エラー";
    }
}