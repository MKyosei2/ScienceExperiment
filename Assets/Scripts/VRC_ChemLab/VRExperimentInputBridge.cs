using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR Input/VRExperimentInputBridge")]
public class VRExperimentInputBridge : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public VRStirDetector stir;
    public VRPourDetector pour;
    public VRShakeDetector shake;

    [Header("Heat (optional)")]
    public bool useSpawnerHeat01 = true; // Heatはspawner側(autoHeatFromProximity)に任せる想定

    [Header("Sync")]
    public float sendInterval = 0.20f;

    private bool active;
    private float nextSendAt;

    public void OnModeVR() { active = true; }
    public void OnModePC()
    {
        active = false;
        // PCに切り替えた瞬間に操作値を落とす
        if (spawner != null) spawner.SetOps01(spawner.GetHeat01(), 0f, 0f, 0f);
    }

    private void Update()
    {
        if (!active) return;
        if (spawner == null) return;

        // 他人が操作者なら送らない（ログ汚染防止）
        if (spawner.HasOperator() && !spawner.IsOperatorLocal()) return;

        if (Time.time < nextSendAt) return;
        nextSendAt = Time.time + sendInterval;

        float stir01 = (stir != null) ? stir.Get01() : 0f;
        float pour01 = (pour != null) ? pour.Get01() : 0f;
        float shake01 = (shake != null) ? shake.Get01() : 0f;

        float heat01 = useSpawnerHeat01 ? spawner.GetHeat01() : 0f;

        spawner.SetOps01(heat01, stir01, pour01, shake01);
    }
}
