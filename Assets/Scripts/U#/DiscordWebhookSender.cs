using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;

public class DiscordWebhookSender : UdonSharpBehaviour
{
    public VRCUrl relayUrl; // 例: https://relay.yourdomain.com/send

    public void SendReactionLog(string logText)
    {
        if (string.IsNullOrWhiteSpace(logText)) return;

        string encoded = VRC.SDKBase.Utilities.UrlEncode(logText);
        string url = $"{relayUrl.Get()}?type=log&content={encoded}";

        VRCStringDownloader.LoadUrl(new VRCUrl(url), this);
    }
}
