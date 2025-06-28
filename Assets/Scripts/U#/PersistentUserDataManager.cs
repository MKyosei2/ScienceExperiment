using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PersistentUserDataManager : UdonSharpBehaviour
{
    private string tagPrefix => Networking.LocalPlayer?.GetPlayerTag("CurrentRoom") ?? "";

    public void SaveLastReaction(string data)
    {
        Networking.LocalPlayer?.SetPlayerTag($"{tagPrefix}_lastReaction", data);
    }

    public string LoadLastReaction()
    {
        return Networking.LocalPlayer?.GetPlayerTag($"{tagPrefix}_lastReaction");
    }
}