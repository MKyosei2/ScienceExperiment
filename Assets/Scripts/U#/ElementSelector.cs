using UdonSharp;
using UnityEngine;

public class ElementSelector : UdonSharpBehaviour
{
    public GameObject elementPrefab;
    public Transform spawnPoint;
    private GameObject currentInstance;

    public override void Interact()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        if (elementPrefab != null && spawnPoint != null)
        {
            currentInstance = VRCInstantiate(elementPrefab);
            currentInstance.transform.position = spawnPoint.position;
            currentInstance.transform.rotation = spawnPoint.rotation;
        }
    }
}
