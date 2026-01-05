using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR/Snap To Points On Drop")]
public class VRSnapToPointsOnDrop : UdonSharpBehaviour
{
    [Header("Snap Targets")]
    public Transform[] snapPoints;
    public float maxDistance = 0.18f;

    [Header("Optional")]
    public Rigidbody rb;
    public AudioSource snapSfx;
    public bool makeKinematicWhenSnapped = false;

    private bool snapped;

    public override void OnPickup()
    {
        // If we were snapped and kinematic, allow movement again
        if (snapped && rb != null) rb.isKinematic = false;
        snapped = false;
    }

    public override void OnDrop()
    {
        if (snapPoints == null || snapPoints.Length == 0) return;

        Transform best = null;
        float bestD = 999f;

        Vector3 p = transform.position;

        for (int i = 0; i < snapPoints.Length; i++)
        {
            var t = snapPoints[i];
            if (t == null) continue;

            float d = Vector3.Distance(p, t.position);
            if (d < bestD)
            {
                bestD = d;
                best = t;
            }
        }

        if (best == null) return;
        if (bestD > maxDistance) return;

        // Snap position/rotation
        transform.SetPositionAndRotation(best.position, best.rotation);

        // Stop rigidbody motion
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (makeKinematicWhenSnapped) rb.isKinematic = true;
        }

        // Play sound
        if (snapSfx != null) snapSfx.Play();

        snapped = true;
    }
}
