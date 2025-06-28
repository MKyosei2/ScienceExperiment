using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using System;

public class PersonallLabNotebook : UdonSharpBehaviour
{
    private const int MaxEntries = 12;
    private string[] notebookEntries = new string[MaxEntries];
    private int entryIndex = 0;

    public Text notebookDisplay;

    public void LogExperiment(string reactionName, string conditionKey)
    {
        string user = "Unknown";
        if (Networking.LocalPlayer != null)
        {
            user = Networking.LocalPlayer.displayName;
        }
        string entry = $"[{DateTime.Now:HH:mm}] {reactionName} @ {conditionKey}";
        notebookEntries[entryIndex] = entry;
        entryIndex = (entryIndex + 1) % MaxEntries;

        UpdateNotebookUI();
    }

    public void UpdateNotebookUI()
    {
        string output = "";
        for (int i = 0; i < MaxEntries; i++)
        {
            int idx = (entryIndex + i) % MaxEntries;
            if (!string.IsNullOrWhiteSpace(notebookEntries[idx]))
            {
                output += notebookEntries[idx] + "\n";
            }
        }
        if (notebookDisplay != null) notebookDisplay.text = output;
    }

    public void ClearNotebook()
    {
        notebookEntries = new string[MaxEntries];
        entryIndex = 0;
        UpdateNotebookUI();
    }
}
