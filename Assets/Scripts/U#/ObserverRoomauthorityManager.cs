using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ObserverRoomauthorityManager : UdonSharpBehaviour
{
    public string observerRoomTag = "ObserverRoomA";
    public int maxObservers = 4;
    public bool isRoomLocked = false;

    [UdonSynced] public int currentOwnerId = -1;

    private VRCPlayerApi[] observers = new VRCPlayerApi[16];
    private int observerCount = 0;

    public void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return;

        if (observerCount >= maxObservers || isRoomLocked)
        {
            if (player.isLocal) Debug.Log("観察室は満員またはロック中です");
            return;
        }

        observers[observerCount++] = player;

        if (currentOwnerId == -1)
        {
            currentOwnerId = player.playerId;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UpdateOwnership));
        }

        player.SetPlayerTag("ObserverAuthority_" + observerRoomTag, player.playerId == currentOwnerId ? "owner" : "viewer");
    }

    public void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return;

        for (int i = 0; i < observerCount; i++)
        {
            if (observers[i] == player)
            {
                for (int j = i; j < observerCount - 1; j++)
                    observers[j] = observers[j + 1];
                observers[observerCount - 1] = null;
                observerCount--;
                break;
            }
        }

        player.SetPlayerTag("ObserverAuthority_" + observerRoomTag, "none");

        if (player.playerId == currentOwnerId)
            AssignNewOwner();
    }

    public void AssignNewOwner()
    {
        currentOwnerId = -1;
        for (int i = 0; i < observerCount; i++)
        {
            if (observers[i] != null && observers[i].IsValid())
            {
                currentOwnerId = observers[i].playerId;
                break;
            }
        }

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UpdateOwnership));
    }

    public void UpdateOwnership()
    {
        for (int i = 0; i < observerCount; i++)
        {
            VRCPlayerApi player = observers[i];
            if (!Utilities.IsValid(player)) continue;
            string role = player.playerId == currentOwnerId ? "owner" : "viewer";
            player.SetPlayerTag("ObserverAuthority_" + observerRoomTag, role);
        }
    }

    public bool IsLocalPlayerOwner()
    {
        return Networking.LocalPlayer != null && Networking.LocalPlayer.playerId == currentOwnerId;
    }

    public int GetObserverCount() => observerCount;
}
