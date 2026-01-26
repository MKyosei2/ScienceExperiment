using UdonSharp;
using UnityEngine;

public class HClExplosionReaction : UdonSharpBehaviour
{
    [Header("Scene References (0128Test配下のものを入れる)")]
    public GameObject flaskH;     // CONICAL_FLASK_H
    public GameObject flaskCl;    // CONICAL_FLASK_Cl
    public Transform beakerPourTarget; // BEAKER_EMPTY の口付近 (子Object推奨)

    [Header("Optional (自動探索もする)")]
    public Transform spoutH;      // フラスコHの注ぎ口(未設定なら自動探索)
    public Transform spoutCl;     // フラスコClの注ぎ口(未設定なら自動探索)

    [Header("Pour 判定")]
    [Tooltip("この角度以上傾けると注いでいる判定になる（0=直立, 90=横, 180=逆さ）")]
    public float pourStartAngleDeg = 35f;

    [Tooltip("注ぎ口がこの距離以内ならBEAKERに入った判定")]
    public float pourRadius = 0.14f;

    [Tooltip("この秒数以上連続で注いだら “投入完了” とみなす")]
    public float requiredPourSeconds = 0.25f;

    [Header("Reaction FX (BEAKER_EMPTY側にまとめる)")]
    public Light explosionLight;
    public float flashSeconds = 0.15f;

    public ParticleSystem explosionParticles;
    public AudioSource explosionAudio;

    [Header("Debug")]
    public bool debugLog = false;

    // 状態
    private bool hasH;
    private bool hasCl;
    private bool reacted;

    private float hPourTimer;
    private float clPourTimer;

    private void Start()
    {
        AutoResolve();
        ResetExperimentState();
    }

    public void ResetExperimentState()
    {
        hasH = false;
        hasCl = false;
        reacted = false;
        hPourTimer = 0f;
        clPourTimer = 0f;

        if (explosionLight != null) explosionLight.enabled = false;
        if (explosionParticles != null) explosionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void AutoResolve()
    {
        // beaker pour target
        if (beakerPourTarget == null)
        {
            beakerPourTarget = FindChildByNameContains(transform, "pourtarget");
            if (beakerPourTarget == null) beakerPourTarget = FindChildByNameContains(transform, "mouth");
            if (beakerPourTarget == null) beakerPourTarget = transform; // fallback
        }

        // spout auto resolve (H)
        if (flaskH != null && spoutH == null)
        {
            spoutH = FindChildByNameContains(flaskH.transform, "spout");
            if (spoutH == null) spoutH = FindChildByNameContains(flaskH.transform, "mouth");
            if (spoutH == null) spoutH = FindChildByNameContains(flaskH.transform, "pour");
            if (spoutH == null) spoutH = flaskH.transform; // fallback
        }

        // spout auto resolve (Cl)
        if (flaskCl != null && spoutCl == null)
        {
            spoutCl = FindChildByNameContains(flaskCl.transform, "spout");
            if (spoutCl == null) spoutCl = FindChildByNameContains(flaskCl.transform, "mouth");
            if (spoutCl == null) spoutCl = FindChildByNameContains(flaskCl.transform, "pour");
            if (spoutCl == null) spoutCl = flaskCl.transform; // fallback
        }
    }

    private void Update()
    {
        if (reacted) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // --- H 投入判定 ---
        if (!hasH && flaskH != null)
        {
            bool pouringH = IsPouringIntoBeaker(flaskH.transform, spoutH);
            if (pouringH)
            {
                hPourTimer += dt;
                if (hPourTimer >= requiredPourSeconds)
                {
                    hasH = true;
                    if (debugLog) Debug.Log("[HClReaction] H 投入完了");
                }
            }
            else
            {
                // 途切れたら少し減衰（完全リセットだとストレスになるので）
                hPourTimer = Mathf.Max(0f, hPourTimer - dt * 2f);
            }
        }

        // --- Cl 投入判定 ---
        if (!hasCl && flaskCl != null)
        {
            bool pouringCl = IsPouringIntoBeaker(flaskCl.transform, spoutCl);
            if (pouringCl)
            {
                clPourTimer += dt;
                if (clPourTimer >= requiredPourSeconds)
                {
                    hasCl = true;
                    if (debugLog) Debug.Log("[HClReaction] Cl 投入完了");
                }
            }
            else
            {
                clPourTimer = Mathf.Max(0f, clPourTimer - dt * 2f);
            }
        }

        // --- 両方入ったら反応 ---
        if (hasH && hasCl)
        {
            React();
        }
    }

    private bool IsPouringIntoBeaker(Transform flaskRoot, Transform spout)
    {
        if (flaskRoot == null || spout == null || beakerPourTarget == null) return false;

        // 傾き判定：フラスコのup と ワールドup の角度
        float angle = Vector3.Angle(flaskRoot.up, Vector3.up);
        if (angle < pourStartAngleDeg) return false;

        // 距離判定：注ぎ口 → ビーカー受け口
        float dist = Vector3.Distance(spout.position, beakerPourTarget.position);
        if (dist > pourRadius) return false;

        return true;
    }

    private void React()
    {
        if (reacted) return;
        reacted = true;

        if (debugLog) Debug.Log("[HClReaction] 反応発生！");

        if (explosionParticles != null)
        {
            explosionParticles.Play(true);
        }

        if (explosionAudio != null)
        {
            explosionAudio.Play();
        }

        if (explosionLight != null)
        {
            explosionLight.enabled = true;
            SendCustomEventDelayedSeconds(nameof(EndFlash), flashSeconds);
        }
    }

    public void EndFlash()
    {
        if (explosionLight != null) explosionLight.enabled = false;
    }

    private Transform FindChildByNameContains(Transform root, string keywordLower)
    {
        if (root == null) return null;
        if (string.IsNullOrEmpty(keywordLower)) return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        if (all == null) return null;

        string key = keywordLower.ToLower();

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;

            string n = t.name;
            if (string.IsNullOrEmpty(n)) continue;

            if (n.ToLower().IndexOf(key) >= 0)
                return t;
        }
        return null;
    }
}
