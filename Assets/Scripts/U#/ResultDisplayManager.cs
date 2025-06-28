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

        if (sharedMonitor != null)
        {
            sharedMonitor.UpdateAllMonitors(message);
        }

        // SE
        if (message.Contains("生成"))
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(successClip);
            }
        }
        else
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(failClip);
            }
        }

        // Discord（匿名ログ）
        if (webhookSender != null)
        {
            webhookSender.SendLog(message, "log");
        }
    }
}