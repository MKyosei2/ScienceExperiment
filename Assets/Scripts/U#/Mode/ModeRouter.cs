using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/ModeRouter")]
public class ModeRouter : UdonSharpBehaviour
{
    [Header("Manual override")]
    public bool manualOverride = true;   // trueなら手動モード
    public bool currentIsVR = false;     // 手動時の現在モード（false=PC / true=VR）

    [Header("Registered targets (auto by spawner)")]
    public ModeActivation[] targets;     // 容量をインスペクタで確保（例: 256）
    public int targetCount = 0;

    public bool IsVR()
    {
        if (manualOverride) return currentIsVR;
        var lp = Networking.LocalPlayer;
        return lp != null && lp.IsUserInVR();
    }

    public void Toggle() { manualOverride = true; currentIsVR = !currentIsVR; ApplyToAll(); }
    public void ForcePC() { manualOverride = true; currentIsVR = false; ApplyToAll(); }
    public void ForceVR() { manualOverride = true; currentIsVR = true; ApplyToAll(); }
    public void SetAuto() { manualOverride = false; ApplyToAll(); }

    public void ApplyToAll()
    {
        bool isVR = IsVR();
        for (int i = 0; i < targetCount; i++)
        {
            var t = targets[i];
            if (t != null) t.ApplyModeFromRouter(this, isVR);
        }
    }

    public void Register(ModeActivation m)
    {
        if (m == null) return;
        // 重複登録防止
        for (int i = 0; i < targetCount; i++) if (targets[i] == m) return;

        if (targets == null || targetCount >= targets.Length) return; // 容量不足時はスキップ
        targets[targetCount] = m;
        targetCount++;
    }
}
