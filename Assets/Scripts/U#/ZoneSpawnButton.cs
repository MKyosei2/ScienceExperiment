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

    [Header("演出プレイヤー（VisualExperimentPlayer）")]
    public VisualExperimentPlayer visualPlayer;

    public override void Interact()
    {
        if (spawnPrefab == null || spawnZone == null) return;

        GameObject instance = VRCInstantiate(spawnPrefab);
        instance.transform.SetPositionAndRotation(spawnZone.position, spawnZone.rotation);

        // 🛑 自身の ZoneSpawnButton を除去
        ZoneSpawnButton zb = instance.GetComponent<ZoneSpawnButton>();
        if (zb != null) Destroy(zb);

        string objectID = spawnPrefab.name;

        // 🔁 選択記録
        if (holder != null)
        {
            if (objectType == "Element")
            {
                holder.AddElement(objectID);
            }
            else if (objectType == "Tool")
            {
                holder.AddTool(objectID);
            }
            else if (objectType == "Condition")
            {
                holder.SetCondition(objectID);
            }
        }

        // 🎥 VisualExperimentPlayer への登録は不要になったため削除
    }
}
