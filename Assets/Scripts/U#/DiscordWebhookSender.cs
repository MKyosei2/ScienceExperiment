using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;

public class DiscordWebhookSender : UdonSharpBehaviour
{
    public VRCUrl relayUrl;   // 例: https://relay.example.com/send

    public void SendLog(string content, string type = "log")
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        string encoded = Utilities.UrlEncode(content);
        string url = $"{relayUrl.Get()}?type={type}&content={encoded}";

        VRCStringDownloader.LoadUrl(new VRCUrl(url), this);
    }
}