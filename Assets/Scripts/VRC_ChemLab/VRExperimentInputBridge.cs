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

    [Header("Motion Fallback (no detectors)")]
    [Tooltip("If VR detectors are missing, estimate ops from the active tool's motion (tilt/rotation/velocity).")]
    public bool useMotionFallback = true;

    public float fallbackPourAngleMinDeg = 10f;
    public float fallbackPourAngleMaxDeg = 80f;
    public float fallbackShakeSpeedScale = 1.5f;
    public float fallbackStirAngularSpeedScale = 180f;
    public float fallbackLerpSpeed = 8f;

    private Transform _fbLastTool;
    private Vector3 _fbLastPos;
    private Quaternion _fbLastRot;
    private bool _fbHasLast;
    private float _fbPour01;
    private float _fbStir01;
    private float _fbShake01;

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

        // Motion fallback (if detectors are missing)
        float fbStir, fbPour, fbShake;
        ComputeFallbackOps(Time.deltaTime, out fbStir, out fbPour, out fbShake);
        if (stir == null) stir01 = fbStir; else if (useMotionFallback) stir01 = Mathf.Max(stir01, fbStir);
        if (pour == null) pour01 = fbPour; else if (useMotionFallback) pour01 = Mathf.Max(pour01, fbPour);
        if (shake == null) shake01 = fbShake; else if (useMotionFallback) shake01 = Mathf.Max(shake01, fbShake);

        float heat01 = useSpawnerHeat01 ? spawner.GetHeat01() : 0f;

        spawner.SetOps01(heat01, stir01, pour01, shake01);
    }

    
    private void ComputeFallbackOps(float dt, out float stir01, out float pour01, out float shake01)
    {
        stir01 = 0f;
        pour01 = 0f;
        shake01 = 0f;

        if (!useMotionFallback) return;
        if (spawner == null) return;

        Transform t = spawner.GetActiveToolTransform();
        if (t == null) return;

        if (_fbLastTool != t)
        {
            _fbLastTool = t;
            _fbHasLast = false;
            _fbPour01 = 0f;
            _fbStir01 = 0f;
            _fbShake01 = 0f;
        }

        if (!_fbHasLast)
        {
            _fbLastPos = t.position;
            _fbLastRot = t.rotation;
            _fbHasLast = true;
            return;
        }

        // Pour: tilt against world up
        float angle = Vector3.Angle(t.up, Vector3.up);
        float targetPour = Mathf.InverseLerp(fallbackPourAngleMinDeg, fallbackPourAngleMaxDeg, angle);

        // Shake: speed magnitude
        Vector3 vel = (t.position - _fbLastPos) / Mathf.Max(0.0001f, dt);
        float speed = vel.magnitude;
        float targetShake = Mathf.Clamp01(speed / Mathf.Max(0.01f, fallbackShakeSpeedScale));

        // Stir: angular speed
        Quaternion dq = t.rotation * Quaternion.Inverse(_fbLastRot);
        float dqAngle;
        Vector3 dqAxis;
        dq.ToAngleAxis(out dqAngle, out dqAxis);
        if (dqAngle > 180f) dqAngle -= 360f;
        float angSpeed = Mathf.Abs(dqAngle) / Mathf.Max(0.0001f, dt);
        float targetStir = Mathf.Clamp01(angSpeed / Mathf.Max(1f, fallbackStirAngularSpeedScale));

        float k = Mathf.Clamp01(fallbackLerpSpeed * dt);
        _fbPour01 = Mathf.Lerp(_fbPour01, targetPour, k);
        _fbShake01 = Mathf.Lerp(_fbShake01, targetShake, k);
        _fbStir01 = Mathf.Lerp(_fbStir01, targetStir, k);

        _fbLastPos = t.position;
        _fbLastRot = t.rotation;

        stir01 = _fbStir01;
        pour01 = _fbPour01;
        shake01 = _fbShake01;
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
