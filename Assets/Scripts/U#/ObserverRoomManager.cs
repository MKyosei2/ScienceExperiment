using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ObserverRoomManager : UdonSharpBehaviour
{
    public string observerRoomID = "ObserverRoomA";

    public void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player == null || !player.IsValid()) return;
        player.SetPlayerTag("CurrentRoom", observerRoomID);
    }

    public void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player == null || !player.IsValid()) return;
        player.SetPlayerTag("CurrentRoom", "None");
    }
}
