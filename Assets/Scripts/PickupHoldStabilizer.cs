using UdonSharp;
using UnityEngine;

public class PickupHoldStabilizer : UdonSharpBehaviour
{
    public Rigidbody rb;

    public override void OnPickup()
    {
        if (rb == null) rb = (Rigidbody)GetComponent(typeof(Rigidbody));
        if (rb == null) return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 掴んだ瞬間に物理を止めると「落ちる/暴れる」が消える
        rb.isKinematic = true;
    }

    public override void OnDrop()
    {
        if (rb == null) rb = (Rigidbody)GetComponent(typeof(Rigidbody));
        if (rb == null) return;

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
