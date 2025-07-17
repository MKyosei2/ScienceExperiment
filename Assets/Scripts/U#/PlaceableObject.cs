using UdonSharp;
using UnityEngine;

public class PlaceableObject : UdonSharpBehaviour
{
    public bool isFixed = false;

    public void OnDrop()
    {
        if (!isFixed)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.magnitude < 0.1f)
            {
                isFixed = true;
                rb.isKinematic = true;
            }
        }
    }
}