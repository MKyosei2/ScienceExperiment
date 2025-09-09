using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/ModeRouter")]
public class ModeRouter : UdonSharpBehaviour
{
    public bool manualOverride = true;
    public bool currentIsVR = false;

    public ModeActivation[] targets;
    public int targetCount = 0;

    public bool IsVR()
    {
        if (manualOverride) return currentIsVR;
        var lp = Networking.LocalPlayer; return (lp != null && lp.IsUserInVR());
    }

    public void Toggle() { manualOverride = true; currentIsVR = !currentIsVR; ApplyToAll(); }
    public void ForcePC() { manualOverride = true; currentIsVR = false; ApplyToAll(); }
    public void ForceVR() { manualOverride = true; currentIsVR = true; ApplyToAll(); }
    public void SetAuto() { manualOverride = false; ApplyToAll(); }

    public void ApplyToAll()
    {
        bool isVR = IsVR();
        for (int i = 0; i < targetCount; i++) { var t = targets[i]; if (t != null) t.ApplyModeFromRouter(this, isVR); }
    }

    public void Register(ModeActivation m)
    {
        if (m == null) return;
        for (int i = 0; i < targetCount; i++) if (targets[i] == m) return;
        if (targets == null || targetCount >= targets.Length) return;
        targets[targetCount] = m; targetCount++;
    }
}
