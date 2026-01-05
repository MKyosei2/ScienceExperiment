using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR Input/VRStirDetector")]
public class VRStirDetector : UdonSharpBehaviour
{
    [Header("Mapping")]
    public float speedFull01 = 0.8f;     // m/s これでStir01=1
    public float smooth = 12f;

    [Header("Runtime")]
    [SerializeField] private float stir01;

    private Vector3 prevPos;
    private float inLiquidUntil;

    private void Start()
    {
        prevPos = transform.position;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 接触が途切れたら inLiquid=false 扱い
        bool inLiquid = Time.time <= inLiquidUntil;

        Vector3 v = (transform.position - prevPos) / dt;
        prevPos = transform.position;

        float target = 0f;
        if (inLiquid)
        {
            float speed = v.magnitude;
            target = Mathf.Clamp01(speed / Mathf.Max(0.0001f, speedFull01));
        }

        // 指数平滑
        stir01 = Mathf.Lerp(stir01, target, 1f - Mathf.Exp(-smooth * dt));
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;

        // CompareTag() はUdonで非対応なので、マーカーの有無で判定
        if (other.GetComponent<ChemLiquidVolumeMarker>() != null)
        {
            inLiquidUntil = Time.time + 0.15f;
        }
    }

    public float Get01() { return stir01; }
}
