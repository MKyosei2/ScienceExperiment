using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneSpawnButton : UdonSharpBehaviour
{
    [Header("Zone種別 (Element / Tool / Condition)")]
    public string objectType = "Element";

    [Header("生成するプレハブ")]
    public GameObject spawnPrefab;

    [Header("生成先（例: ElementExperimentZone）")]
    public Transform spawnZone;

    [Header("データ記録先")]
    public SelectedObjectHolder holder;

    public override void Interact()
    {
        if (spawnPrefab == null || spawnZone == null) return;

        GameObject instance = VRCInstantiate(spawnPrefab);
        instance.transform.SetPositionAndRotation(spawnZone.position, spawnZone.rotation);
        instance.transform.SetParent(spawnZone); // 子として追加

        // 🛑 再帰防止
        ZoneSpawnButton zb = instance.GetComponent<ZoneSpawnButton>();
        if (zb != null) Destroy(zb);

        // 🚫 Pickup無効
        VRC_Pickup pickup = (VRC_Pickup)instance.GetComponent(typeof(VRC_Pickup));
        if (pickup != null) pickup.pickupable = false;

        // 📝 選択登録
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

        Debug.Log($"✅ {objectType} {id} を {spawnZone.name} に生成（子として追加）");
    }
}
