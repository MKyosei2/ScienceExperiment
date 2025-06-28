using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PersistentUserDataManager : UdonSharpBehaviour
{
    private string tagPrefix = "";

    void Start()
    {
        if (Networking.LocalPlayer != null)
        {
            string tag = Networking.LocalPlayer.GetPlayerTag("CurrentRoom");
            tagPrefix = tag != null ? tag : "";
        }
    }

    public void SaveLastReaction(string data)
    {
        if (Networking.LocalPlayer != null)
        {
            Networking.LocalPlayer.SetPlayerTag(tagPrefix + "_lastReaction", data);
        }
    }

    public string LoadLastReaction()
    {
        if (Networking.LocalPlayer != null)
        {
            return Networking.LocalPlayer.GetPlayerTag(tagPrefix + "_lastReaction");
        }
        return "";
    }
}