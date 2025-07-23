using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneSpawnButton : UdonSharpBehaviour
{
    public string objectType = "Element"; // "Tool", "Condition"
    public GameObject spawnPrefab;
    public Transform spawnZone;
    public SelectedObjectHolder holder;

    public override void Interact()
    {
        if (spawnPrefab == null || spawnZone == null) return;

        GameObject instance = VRCInstantiate(spawnPrefab);
        instance.transform.SetPositionAndRotation(spawnZone.position, spawnZone.rotation);

        // 再帰防止・Pickup無効化
        Destroy(instance.GetComponent<ZoneSpawnButton>());
        var pickup = instance.GetComponent<VRC_Pickup>();
        if (pickup) pickup.pickupable = false;

        // 選択状態として登録
        string id = spawnPrefab.name;
        if (holder != null)
        {
            switch (objectType)
            {
                case "Element": holder.AddElement(id); break;
                case "Tool": holder.AddTool(id); break;
                case "Condition": holder.SetCondition(id); break;
            }
        }

        Debug.Log($"✅ {objectType} {id} を {spawnZone.name} に生成し、選択登録");
    }
}
