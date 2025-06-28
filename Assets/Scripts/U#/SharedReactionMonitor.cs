using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class SharedReactionMonitor : UdonSharpBehaviour
{
    public Text[] monitorDisplays;
    public string currentReactionText = "";

    public void UpdateAllMonitors(string newText)
    {
        currentReactionText = newText;
        foreach (Text t in monitorDisplays)
        {
            if (t != null) t.text = newText;
        }
    }

    public void ClearAllMonitors()
    {
        currentReactionText = "";
        foreach (Text t in monitorDisplays)
        {
            if (t != null) t.text = "";
        }
    }

    public void RefreshForLocalPlayer()
    {
        UpdateAllMonitors(currentReactionText);
    }
}
