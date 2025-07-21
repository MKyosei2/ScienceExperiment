using UdonSharp;
using UnityEngine;

public class ConditionSelector : UdonSharpBehaviour
{
    public GameObject conditionPrefab;
    public Transform spawnPoint;
    private GameObject currentInstance;

    [Header("クリア対象（ExperimentZone内の生成済みオブジェクト）")]
    public GameObject[] existingObjects;

    public override void Interact()
    {
        // 1. 既存オブジェクト削除
        foreach (GameObject obj in existingObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // 2. Condition生成
        if (conditionPrefab != null && spawnPoint != null)
        {
            currentInstance = VRCInstantiate(conditionPrefab);
            currentInstance.transform.position = spawnPoint.position;
            currentInstance.transform.rotation = spawnPoint.rotation;
        }
    }
}
