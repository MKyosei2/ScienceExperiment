using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class RoomAccessLimiter : UdonSharpBehaviour
{
    public string roomName = "ExperimentRoom1";
    public int maxPlayers = 2;
    public int currentOccupancy = 0;

    public void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (currentOccupancy >= maxPlayers) return;

        currentOccupancy++;
        player.SetPlayerTag("CurrentRoom", roomName);
    }

    public void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        currentOccupancy--;
        if (player.IsValid()) player.SetPlayerTag("CurrentRoom", "None");
    }

    public bool IsRoomOccupied()
    {
        return currentOccupancy > 0;
    }
}
