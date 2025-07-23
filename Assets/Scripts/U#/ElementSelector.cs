using UdonSharp;
using UnityEngine;

public class ElementSelector : UdonSharpBehaviour
{
    public GameObject elementPrefab;
    public Transform elementExperimentZone;
    private GameObject currentInstance;

    public override void Interact()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        if (elementPrefab != null && elementExperimentZone != null)
        {
            currentInstance = VRCInstantiate(elementPrefab);
            currentInstance.transform.position = elementExperimentZone.position;
            currentInstance.transform.rotation = elementExperimentZone.rotation;
        }
    }
}