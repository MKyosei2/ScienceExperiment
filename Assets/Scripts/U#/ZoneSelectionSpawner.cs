using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using System.IO;
using TMPro;

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

        if (currentSpawned != null)
        {
            Destroy(currentSpawned);
        }

        currentSpawned = VRCInstantiate(spawnablePrefab);
        currentSpawned.transform.SetPositionAndRotation(spawnTarget.position, spawnTarget.rotation);

        SelectorObject newSel = currentSpawned.GetComponent<SelectorObject>();
        if (newSel != null)
        {
            newSel.SetObjectTypeAndID(selector.GetObjectType(), selector.GetObjectID());
        }
    }

    void OnTriggerExit(Collider other)
    {
        Destroy(currentSpawned);
    }
}