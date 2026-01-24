using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR Input/VRPourDetector")]
public class VRPourDetector : UdonSharpBehaviour
{
    public Transform spout;
    public Transform target;

    [Header("Auto Resolve (optional)")]
    [Tooltip("spout/target が未設定でも動くように自動探索します。")]
    public bool autoResolve = true;

    [Header("Pour Conditions")]
    public float startAngleDeg = 35f;  // これ以上傾けたら出始める
    public float fullAngleDeg = 85f;  // これでPour01=1
    public float maxDistance = 0.18f; // 注ぎ口がこれ以上離れたら無効

    [Header("Smoothing")]
    public float smooth = 10f;

    [SerializeField] private float pour01;

    private void Start()
    {
        if (!autoResolve) return;

        // target: scene object 'PourTargetPoint' (exists in the default scene)
        if (target == null)
        {
            GameObject t = GameObject.Find("PourTargetPoint");
            if (t != null) target = t.transform;

            // Fallback: if the scene doesn't have PourTargetPoint,
            // try to use the experiment container (ChemElementSpawner.containerTransform).
            if (target == null)
            {
                ChemElementSpawner sp = GetComponentInParent<ChemElementSpawner>();
                if (sp != null && sp.containerTransform != null)
                {
                    target = sp.containerTransform;
                }
            }
        }

        // spout: prefer children named like Spout/Mouth/Pour
        if (spout == null)
        {
            spout = FindChildByNameContains(transform, "spout");
            if (spout == null) spout = FindChildByNameContains(transform, "mouth");
            if (spout == null) spout = FindChildByNameContains(transform, "pour");
            if (spout == null) spout = transform; // fallback
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float target01 = 0f;
        if (spout != null && target != null)
        {
            float d = Vector3.Distance(spout.position, target.position);
            if (d <= maxDistance)
            {
                float angle = Vector3.Angle(transform.up, Vector3.up); // upright=0, upside-down=180
                float t = Mathf.InverseLerp(startAngleDeg, fullAngleDeg, angle);
                target01 = Mathf.Clamp01(t);
            }
        }

        pour01 = Mathf.Lerp(pour01, target01, 1f - Mathf.Exp(-smooth * dt));
    }

    public float Get01() { return pour01; }

    private Transform FindChildByNameContains(Transform root, string key)
    {
        if (root == null || string.IsNullOrEmpty(key)) return null;
        string ku = key.ToUpper();

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        if (all == null) return null;
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t == root) continue;
            string n = t.name;
            if (string.IsNullOrEmpty(n)) continue;
            if (n.ToUpper().IndexOf(ku) >= 0) return t;
        }
        return null;
    }
}
