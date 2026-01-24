using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// ToolMotionOpsEstimator (B-plan)
/// --------------------------------
/// VRChat用の専用Detectorを置かなくても、掴まれている器具のTransformの動きから
/// Pour / Stir / Shake の操作量(0..1)を推定して ChemElementSpawner.SetOps01() に渡す補助コンポーネント。
///
/// 使い方:
/// 1) ChemElementSpawner と同じ GameObject か、任意の管理Objectに付ける
/// 2) spawner をInspectorで指定（必須）
/// 3) toolOverride を指定しない場合、spawner.GetActiveToolTransform() を参照します
/// </summary>
public class ToolMotionOpsEstimator : UdonSharpBehaviour
{
    [Header("References")]
    public ChemElementSpawner spawner;
    public Transform toolOverride;

    [Header("Tuning")]
    public float pourAngleMinDeg = 10f;
    public float pourAngleMaxDeg = 80f;

    [Tooltip("Shakeの強さ推定に使う速度(m/s)のスケール。大きいほど振っても値が上がりにくい。")]
    public float shakeSpeedScale = 1.5f;

    [Tooltip("Stirの強さ推定に使う角速度(deg/s)のスケール。大きいほど回しても値が上がりにくい。")]
    public float stirAngularSpeedScale = 180f;

    [Header("Smoothing")]
    public float lerpSpeed = 8f;

    private Vector3 _lastPos;
    private Quaternion _lastRot;
    private bool _hasLast;

    private float _pour01;
    private float _stir01;
    private float _shake01;

    private Transform _lastTool;

    private void Update()
    {
        if (spawner == null) return;

        // FIX (2026-01):
        // The previous implementation required the local player to be the *network owner* of the spawner
        // before motion ops were processed. In VRChat this often stays false (especially right after join),
        // which means Pour/Stir/Shake never update in VR.
        //
        // Spawner.SetOps01() already calls EnsureCanControl() internally (and will claim ownership when needed),
        // so we must NOT early-return here.
        if (spawner.HasOperator() && !spawner.IsOperatorLocal()) return;

        Transform t = toolOverride;
        if (t == null)
            t = spawner.GetActiveToolTransform();

        if (t == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // If active tool changed, reset history so we don't spike values.
        if (_lastTool != t)
        {
            _lastTool = t;
            _hasLast = false;
        }

        if (!_hasLast)
        {
            _lastPos = t.position;
            _lastRot = t.rotation;
            _hasLast = true;
            return;
        }

        // --- Pour: 傾き（ワールドUpとの角度） ---
        // 0..1: ほぼ水平=0 / かなり傾いた=1
        float angle = Vector3.Angle(t.up, Vector3.up);
        float targetPour = Mathf.InverseLerp(pourAngleMinDeg, pourAngleMaxDeg, angle);

        // --- Shake: 速度（位置デルタ） ---
        Vector3 vel = (t.position - _lastPos) / dt;
        float speed = vel.magnitude;
        float targetShake = Mathf.Clamp01(speed / Mathf.Max(0.01f, shakeSpeedScale));

        // --- Stir: 角速度（回転デルタ） ---
        Quaternion dq = t.rotation * Quaternion.Inverse(_lastRot);
        float dqAngle;
        Vector3 dqAxis;
        dq.ToAngleAxis(out dqAngle, out dqAxis);
        if (dqAngle > 180f) dqAngle -= 360f;
        float angSpeed = Mathf.Abs(dqAngle) / dt; // deg/s
        float targetStir = Mathf.Clamp01(angSpeed / Mathf.Max(1f, stirAngularSpeedScale));

        // Smooth
        _pour01 = Mathf.Lerp(_pour01, targetPour, Mathf.Clamp01(lerpSpeed * dt));
        _shake01 = Mathf.Lerp(_shake01, targetShake, Mathf.Clamp01(lerpSpeed * dt));
        _stir01 = Mathf.Lerp(_stir01, targetStir, Mathf.Clamp01(lerpSpeed * dt));

        // Heatは既存入力/温度系に委ねる（ここでは維持）
        float heat01 = spawner.GetHeat01();

        spawner.SetOps01(heat01, _stir01, _pour01, _shake01);

        _lastPos = t.position;
        _lastRot = t.rotation;
    }

    public void ResetState()
    {
        _hasLast = false;
        _pour01 = 0f;
        _shake01 = 0f;
        _stir01 = 0f;
    }
}
