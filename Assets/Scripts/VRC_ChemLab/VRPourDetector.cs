using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR Input/VRPourDetector")]
public class VRPourDetector : UdonSharpBehaviour
{
    public Transform spout;
    public Transform target;

    [Header("Pour Conditions")]
    public float startAngleDeg = 35f;  // これ以上傾けたら出始める
    public float fullAngleDeg = 85f;  // これでPour01=1
    public float maxDistance = 0.18f; // 注ぎ口がこれ以上離れたら無効

    [Header("Smoothing")]
    public float smooth = 10f;

    [SerializeField] private float pour01;

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
}
