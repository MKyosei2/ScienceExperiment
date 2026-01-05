using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR Input/VRShakeDetector")]
public class VRShakeDetector : UdonSharpBehaviour
{
    [Header("Mapping")]
    public float accelFull01 = 8.0f;  // m/s^2 これでShake01=1
    public float smooth = 10f;

    [SerializeField] private float shake01;

    private Vector3 prevPos;
    private Vector3 prevVel;

    private void Start()
    {
        prevPos = transform.position;
        prevVel = Vector3.zero;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 vel = (transform.position - prevPos) / dt;
        Vector3 acc = (vel - prevVel) / dt;

        prevPos = transform.position;
        prevVel = vel;

        float a = acc.magnitude;
        float target01 = Mathf.Clamp01(a / Mathf.Max(0.0001f, accelFull01));

        shake01 = Mathf.Lerp(shake01, target01, 1f - Mathf.Exp(-smooth * dt));
    }

    public float Get01() { return shake01; }
}
