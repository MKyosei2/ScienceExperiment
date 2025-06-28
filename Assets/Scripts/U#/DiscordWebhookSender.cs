using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;

public class DiscordWebhookSender : UdonSharpBehaviour
{
    public VRCUrl relayUrl;   // 例: https://relay.example.com/send

    // 簡易的なURLエンコード関数
    private string SimpleUrlEncode(string str)
    {
        return str.Replace(" ", "%20").Replace("!", "%21").Replace("#", "%23")
                  .Replace("$", "%24").Replace("&", "%26").Replace("'", "%27")
                  .Replace("(", "%28").Replace(")", "%29").Replace("*", "%2A")
                  .Replace("+", "%2B").Replace(",", "%2C").Replace("/", "%2F")
                  .Replace(":", "%3A").Replace(";", "%3B").Replace("=", "%3D")
                  .Replace("?", "%3F").Replace("@", "%40").Replace("[", "%5B")
                  .Replace("]", "%5D");
    }

    public void SendLog(string content, string type = "log")
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        string encoded = SimpleUrlEncode(content);
        string url = $"{relayUrl.Get()}?type={type}&content={encoded}";

        // UdonBehaviour を取得して渡す
        var udon = (UdonBehaviour)GetComponent(typeof(UdonBehaviour));
        VRCStringDownloader.LoadUrl(new VRCUrl(url), udon);
    }
}