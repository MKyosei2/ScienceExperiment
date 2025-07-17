using UdonSharp;
using UnityEngine;

public class VRExperimentMonitor : UdonSharpBehaviour
{
    public string[] actionLog = new string[16];
    public int actionCount = 0;

    public void LogAction(string toolName)
    {
        if (actionCount < actionLog.Length)
        {
            actionLog[actionCount++] = toolName + ":" + Time.time.ToString("F2");
        }
    }

    public string GetLogText()
    {
        return string.Join(",", actionLog);
    }

    public void ClearLog()
    {
        for (int i = 0; i < actionLog.Length; i++) actionLog[i] = "";
        actionCount = 0;
    }
}