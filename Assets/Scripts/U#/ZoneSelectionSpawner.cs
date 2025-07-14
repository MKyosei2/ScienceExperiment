using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneSelectionSpawner : UdonSharpBehaviour
{
    [Header("判定するゾーンの種類")]
    public string zoneType = "Element"; // Element, Tool, Condition

    [Header("生成先（1個だけ配置）")]
    public Transform spawnTarget;

    [Header("対象となるボタン化Prefab（選択用）")]
    public GameObject spawnablePrefab;

    private GameObject currentSpawned;

    void OnTriggerEnter(Collider other)
    {
        SelectorObject selector = other.GetComponent<SelectorObject>();
        if (selector == null) return;

        if (!selector.GetObjectType().Equals(zoneType)) return;

        // 既存のものがあれば削除
        if (currentSpawned != null)
        {
            Destroy(currentSpawned);
        }

        // 生成して指定位置に置く
        currentSpawned = VRCInstantiate(spawnablePrefab);
        currentSpawned.transform.SetPositionAndRotation(spawnTarget.position, spawnTarget.rotation);

        // IDをコピーする（SelectorObjectがあれば）
        SelectorObject newSel = currentSpawned.GetComponent<SelectorObject>();
        if (newSel != null)
        {
            newSel.SetObjectTypeAndID(selector.GetObjectType(), selector.GetObjectID());
        }
    }

    void OnTriggerExit(Collider other)
    {
        // 退去時に削除
        Destroy(currentSpawned);
    }
}
