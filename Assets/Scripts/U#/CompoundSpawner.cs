using UdonSharp;
using UnityEngine;

public class CompoundSpawner : UdonSharpBehaviour
{
    public GameObject[] stylePrefabs;
    public Transform spawnPoint;

    public void SpawnCompound(string name, int styleIndex)
    {
        if (stylePrefabs == null || stylePrefabs.Length == 0) return;

        GameObject prefab = stylePrefabs[styleIndex % stylePrefabs.Length];
        GameObject instance = VRCInstantiate(prefab);

        instance.name = name;
        instance.transform.position = spawnPoint != null ? spawnPoint.position : transform.position;
    }
}
