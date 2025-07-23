using UdonSharp;
using UnityEngine;

public class ToolSelector : UdonSharpBehaviour
{
    public GameObject toolPrefab;
    public Transform toolExperimentZone;
    private GameObject currentInstance;

    public override void Interact()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        if (toolPrefab != null && toolExperimentZone != null)
        {
            currentInstance = VRCInstantiate(toolPrefab);
            currentInstance.transform.position = toolExperimentZone.position;
            currentInstance.transform.rotation = toolExperimentZone.rotation;
        }
    }
}
