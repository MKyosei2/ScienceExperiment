using UdonSharp;
using UnityEngine;

public class CompoundPrefabAssembler : UdonSharpBehaviour
{
    public Transform spawnPoint;
    public GameObject[] stylePrefabs;
    public EnvironmentController environmentController;

    public void GenerateCompound(string compoundName, int styleIndex)
    {
        if (stylePrefabs == null || stylePrefabs.Length == 0) return;

        GameObject prefab = stylePrefabs[styleIndex % stylePrefabs.Length];
        GameObject instance = VRCInstantiate(prefab);

        instance.transform.position = spawnPoint != null
                                    ? spawnPoint.position
                                    : transform.position;

        instance.name = compoundName;

        // 行動制御スクリプトに環境情報を渡す
        CompoundBehaviorController beh = instance.GetComponent<CompoundBehaviorController>();
        if (beh != null)
        {
            beh.environmentController = environmentController;
            beh.ApplyBehavior();
        }
    }
}