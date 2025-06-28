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

    public DiscordWebhookSender webhookSender;

    public void ShowResult(string message)
    {
        // UI 更新
        foreach (Text t in observerPanels)
            if (t != null) t.text = message;

        sharedMonitor?.UpdateAllMonitors(message);

        // SE
        if (message.Contains("生成"))
            audioSource?.PlayOneShot(successClip);
        else
            audioSource?.PlayOneShot(failClip);

        // Discord（匿名ログ）
        webhookSender?.SendLog(message, "log");
    }
}