using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class MonitorSwitchRouter : UdonSharpBehaviour
{
    public RenderTexture[] experimentRoomFeeds;
    public RawImage targetMonitor;
    public Text monitorLabel;

    private int currentIndex = -1;

    public void SwitchMonitorToRoom(string roomName)
    {
        int newIndex = ParseRoomIndex(roomName);
        if (newIndex < 0 || newIndex >= experimentRoomFeeds.Length) return;

        targetMonitor.texture = experimentRoomFeeds[newIndex];
        monitorLabel.text = $"観察中：{roomName}";
        currentIndex = newIndex;
    }

    private int ParseRoomIndex(string roomName)
    {
        if (roomName.StartsWith("実験室"))
        {
            string numPart = roomName.Substring(3);
            if (int.TryParse(numPart, out int idx)) return idx - 1;
        }
        return -1;
    }
}
