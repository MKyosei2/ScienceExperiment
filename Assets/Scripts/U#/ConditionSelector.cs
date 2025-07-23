using UdonSharp;
using UnityEngine;

public class ConditionSelector : UdonSharpBehaviour
{
    public GameObject conditionPrefab;
    public Transform conditionExperimentZone;
    public GameObject[] existingObjects;
    private GameObject currentInstance;

    public override void Interact()
    {
        // 既存のElement/Tool/Conditionを削除
        foreach (GameObject obj in existingObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // Condition生成
        if (conditionPrefab != null && conditionExperimentZone != null)
        {
            currentInstance = VRCInstantiate(conditionPrefab);
            currentInstance.transform.position = conditionExperimentZone.position;
            currentInstance.transform.rotation = conditionExperimentZone.rotation;
        }
    }
}
