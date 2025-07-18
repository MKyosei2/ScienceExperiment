using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PickupZoneRestrictor : UdonSharpBehaviour
{
    public GameObject[] zoneObjects; // 対象となるゾーン（例: ElementExperimentZone など）

    private VRC_Pickup pickup;

    void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        UpdatePickupable();
    }

    void Update()
    {
        UpdatePickupable();
    }

    void UpdatePickupable()
    {
        if (pickup == null || zoneObjects == null || zoneObjects.Length == 0) return;

        for (int i = 0; i < zoneObjects.Length; i++)
        {
            GameObject zone = zoneObjects[i];
            if (zone == null) continue;
            Collider col = (Collider)zone.GetComponent(typeof(Collider));
            if (col != null && col.bounds.Contains(transform.position))
            {
                pickup.pickupable = false;
                return;
            }
        }

        pickup.pickupable = true;
    }
}