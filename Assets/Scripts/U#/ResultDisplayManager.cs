using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ResultDisplayManager : UdonSharpBehaviour
{
    public Text[] observerPanels;
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip failClip;
    public SharedReactionMonitor sharedMonitor;

    public DiscordWebhookSender webhookSender; // 追加

    public void ShowResult(string message)
    {
        foreach (Text t in observerPanels)
        {
            if (t != null) t.text = message;
        }

        sharedMonitor?.UpdateAllMonitors(message);

        if (message.Contains("生成"))
            audioSource?.PlayOneShot(successClip);
        else
            audioSource?.PlayOneShot(failClip);

        // Discordへログ送信
        if (webhookSender != null)
        {
            webhookSender.SendReactionLog(message);
        }
    }
}
