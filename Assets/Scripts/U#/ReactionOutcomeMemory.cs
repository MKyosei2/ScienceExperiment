using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ReactionOutcomeMemory : UdonSharpBehaviour
{
    [UdonSynced] public string[] reactionLogs = new string[10];
    private int logIndex = 0;

    public Text logListDisplay;

    public void RecordNewReaction(string reactionTitle, string byPlayerName, string condition)
    {
        string log = $"[{Time.realtimeSinceStartup:F0}s] {byPlayerName} - {reactionTitle} ({condition})";

        reactionLogs[logIndex] = log;
        logIndex = (logIndex + 1) % reactionLogs.Length;

        RequestSerialization();
        UpdateDisplay();
    }

    public override void OnDeserialization()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (logListDisplay == null) return;

        string full = "";
        foreach (var entry in reactionLogs)
        {
            if (!string.IsNullOrWhiteSpace(entry))
                full += entry + "\n";
        }

        logListDisplay.text = full;
    }
}
