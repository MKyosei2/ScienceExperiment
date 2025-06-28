using UdonSharp;
using UnityEngine;

public class CompoundPrefabAssembler : UdonSharpBehaviour
{
    public Transform spawnPoint;
    public GameObject[] stylePrefabs;
    public EnvironmentController environmentController;

    public void GenerateCompound(string name, int styleIndex)
    {
        GameObject prefab = stylePrefabs[styleIndex % stylePrefabs.Length];
        GameObject instance = VRCInstantiate(prefab);

        instance.transform.position = spawnPoint.position;
        instance.name = name;

        CompoundBehaviorController behavior = instance.GetComponent<CompoundBehaviorController>();
        if (behavior != null)
        {
            behavior.environmentController = environmentController;
            behavior.ApplyBehavior();
        }
    }
}
