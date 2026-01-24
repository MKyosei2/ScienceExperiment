using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

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

    [Header("Auto Resolve (optional)")]
    [Tooltip("Inspector参照が未設定でも動くように、容器付近からDetectorを自動探索します。")]
    public bool autoResolveDetectors = true;

    [Tooltip("自動探索の実行間隔(秒)。Updateごとに探索すると重いため間引きします。")]
    public float resolveInterval = 1.0f;

    private bool active;
    private float nextSendAt;
    private float nextResolveAt;

    private void Start()
    {
        // Robust fallback:
        // If ModeActivation/Router isn't present or notify arrays are not wired,
        // OnModeVR/OnModePC may never be called.
        // In that case, detect the current platform once and behave correctly.
        var lp = Networking.LocalPlayer;
        if (lp != null)
        {
            active = lp.IsUserInVR();
        }
    }

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

        // Auto resolve missing detectors (safe fallback)
        if (autoResolveDetectors && Time.time >= nextResolveAt)
        {
            nextResolveAt = Time.time + Mathf.Max(0.2f, resolveInterval);
            TryAutoResolveDetectors();
        }

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

    private void TryAutoResolveDetectors()
    {
        // Prefer searching around the experiment container (VR_StartZone)
		Transform searchRoot = (spawner != null && spawner.containerTransform != null) ? spawner.containerTransform : null;
		if (searchRoot == null) searchRoot = transform.root;
		if (searchRoot == null) searchRoot = transform;

        if (stir == null)
        {
			// UdonSharpBehaviour does not support generic method declarations,
			// so we keep this resolution simple & explicit.
			stir = searchRoot.GetComponentInChildren<VRStirDetector>(true);
        }

        if (pour == null)
        {
			pour = searchRoot.GetComponentInChildren<VRPourDetector>(true);
        }

        if (shake == null)
        {
			shake = searchRoot.GetComponentInChildren<VRShakeDetector>(true);
        }
    }
}
