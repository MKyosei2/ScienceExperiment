using UdonSharp;
using UnityEngine;
using TMPro;

public class VRExperimentMonitor : UdonSharpBehaviour
{
    [Header("ログ出力UI")]
    public TextMeshProUGUI logUI;

    [Header("最新ログ数")]
    public int maxLines = 5;
    private string[] logHistory;
    private int index = 0;

    void Start()
    {
        logHistory = new string[maxLines];
    }

    public void Log(string message)
    {
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        string full = $"[{time}] {message}";

        Debug.Log($"[VRMonitor] {full}");

        logHistory[index % maxLines] = full;
        index++;

        if (logUI != null)
        {
            logUI.text = "";
            for (int i = 0; i < maxLines; i++)
            {
                int idx = (index + i) % maxLines;
                if (!string.IsNullOrEmpty(logHistory[idx]))
                    logUI.text += logHistory[idx] + "\n";
            }
        }
    }
}
