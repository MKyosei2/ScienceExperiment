using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class LobbyStatusMonitor : UdonSharpBehaviour
{
    public Text[] experimentRoomTexts;
    public Text[] observerRoomTexts;

    public RoomAccessLimiter[] experimentRooms;
    public ObserverRoomauthorityManager[] observerRooms;

    public float refreshInterval = 3f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= refreshInterval)
        {
            RefreshStatusDisplay();
            timer = 0f;
        }
    }

    public void RefreshStatusDisplay()
    {
        for (int i = 0; i < experimentRooms.Length && i < experimentRoomTexts.Length; i++)
        {
            string status = experimentRooms[i].IsRoomOccupied() ? "使用中" : "空室";
            experimentRoomTexts[i].text = $"実験室{i + 1}: {status}";
        }

        for (int i = 0; i < observerRooms.Length && i < observerRoomTexts.Length; i++)
        {
            int count = observerRooms[i].GetObserverCount();
            bool locked = observerRooms[i].isRoomLocked;
            string lockStatus = locked ? "🔒" : "";
            observerRoomTexts[i].text = $"観察室{i + 1}: 観察者{count}人 {lockStatus}";
        }
    }
}
