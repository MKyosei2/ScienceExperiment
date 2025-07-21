using UdonSharp;
using UnityEngine;

public class ToolSelector : UdonSharpBehaviour
{
    public GameObject toolPrefab;
    public Transform spawnPoint;
    private GameObject currentInstance;

    public override void Interact()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        if (toolPrefab != null && spawnPoint != null)
        {
            currentInstance = VRCInstantiate(toolPrefab);
            currentInstance.transform.position = spawnPoint.position;
            currentInstance.transform.rotation = spawnPoint.rotation;
        }
    }
}
