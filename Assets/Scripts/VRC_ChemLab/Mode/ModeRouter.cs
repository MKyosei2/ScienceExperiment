using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ModeRouter : UdonSharpBehaviour
{
    private bool forcePC = false;
    private bool registered = false;

    // ← ModeActivationが呼び出す想定のRegister()を復活
    public void Register(object target = null)
    {
        if (registered) return;
        registered = true;
        Debug.Log("[ModeRouter] Register called (target=" + (target != null ? target.ToString() : "null") + ")");
    }

    public bool IsVR()
    {
        if (forcePC) return false;
        var lp = Networking.LocalPlayer;
        if (lp == null) return false;
        return lp.IsUserInVR();
    }

    public void Toggle()
    {
        forcePC = !forcePC;
        Debug.Log("[ModeRouter] Mode: " + (forcePC ? "PC" : "VR"));
    }
}
