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
    public VRCUrl apiBase;

    public void SendToAI(string[] elements, string conditionKey)
    {
        if (builder == null) return;

        string query = builder.BuildQuery(elements, conditionKey);
        statusDisplay.text = "AIに送信中…";

        if (statusDisplay != null) statusDisplay.text = "AIに送信中…";
        VRCStringDownloader.LoadUrl(apiBase, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        if (parser != null)
        {
            parser.ParseResponse(result.Result);
        }
        if (statusDisplay != null) statusDisplay.text = "完了";
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        if (statusDisplay != null) statusDisplay.text = "AI応答エラー";
    }
}