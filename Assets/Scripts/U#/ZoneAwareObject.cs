using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneAwareObject : UdonSharpBehaviour
{
    public GameObject spawnPrefab;
    private bool inSelectionZone = false;
    private bool inExperimentZone = false;

    private void Update()
    {
        inSelectionZone = IsInsideZoneWithTag("SelectionZone");
        inExperimentZone = IsInsideZoneWithTag("ExperimentZone");

        // グラブ可否を切り替え
        var pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        if (pickup != null)
        {
            pickup.pickupable = inExperimentZone;
        }
    }

    public override void Interact()
    {
        if (inSelectionZone)
        {
            Debug.Log("📦 選択ゾーン内 → スポーンのみ");
            if (spawnPrefab != null)
            {
                GameObject spawned = VRCInstantiate(spawnPrefab);
                spawned.transform.position = transform.position + Vector3.right * 0.2f;
            }
        }
        else
        {
            Debug.Log("🧤 実験ゾーン or その他 → 通常の掴み動作");
            // Grabbableであれば掴める
        }
    }

    private bool IsInsideZoneWithTag(string tag)
    {
        GameObject[] zones = GameObject.FindGameObjectsWithTag(tag);
        for (int i = 0; i < zones.Length; i++)
        {
            Collider zoneCollider = zones[i].GetComponent<Collider>();
            if (zoneCollider != null && zoneCollider.bounds.Contains(transform.position))
            {
                return true;
            }
        }
        return false;
    }
}
