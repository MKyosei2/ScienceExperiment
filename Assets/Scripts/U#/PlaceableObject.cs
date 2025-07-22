using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlaceableObject : UdonSharpBehaviour
{
    public bool isFixed = false;

    [Header("ログ出力先（任意）")]
    public VRExperimentMonitor monitor;

    public override void OnPickup()
    {
        if (monitor != null)
        {
            monitor.Log("🖐 " + gameObject.name + " を掴んだ");
        }
    }

    public void OnDrop()
    {
        if (!isFixed)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.magnitude < 0.1f)
            {
                isFixed = true;
                if (rb != null) rb.isKinematic = true;

                if (monitor != null)
                {
                    monitor.Log("🧪 " + gameObject.name + " を置いた（固定）");
                }
            }
        }
    }
}
