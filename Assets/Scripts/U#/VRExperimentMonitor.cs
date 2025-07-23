using UdonSharp;
using UnityEngine;
using TMPro;

public class VRExperimentMonitor : UdonSharpBehaviour
{
    [Header("ログ出力UI")]
    public TextMeshProUGUI logText;

    [Header("最大履歴数")]
    public int maxLogs = 10;

    private string[] logs = new string[10];
    private int logIndex = 0;

    public void Log(string message)
    {
        if (logs.Length != maxLogs)
        {
            logs = new string[maxLogs];
        }

        logs[logIndex] = message;
        logIndex = (logIndex + 1) % maxLogs;

        UpdateLogDisplay();
    }

    private void UpdateLogDisplay()
    {
        string combined = "";
        int count = 0;
        for (int i = 0; i < maxLogs; i++)
        {
            int idx = (logIndex + i) % maxLogs;
            string entry = logs[idx];
            if (!string.IsNullOrEmpty(entry))
            {
                combined += "• " + entry + "\n";
                count++;
            }
        }

        if (logText != null)
        {
            logText.text = "📜 実験ログ（最新 " + count + " 件）\n" + combined;
        }
    }
}
