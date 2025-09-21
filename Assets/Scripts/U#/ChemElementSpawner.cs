using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("▼ 生成するフラスコ（複数登録可）")]
    public GameObject[] flaskPrefabs;

    [Header("▼ 生成位置/親")]
    public Transform spawnPoint;
    public Transform parentRoot;

    [Header("▼ 共通見た目制御")]
    public ChemVisualController visualController;

    public void SpawnElement(int prefabId, int elementId)
    {
        if (prefabId < 0 || prefabId >= flaskPrefabs.Length) return;
        GameObject prefab = flaskPrefabs[prefabId];
        if (prefab == null) return;

        GameObject flask = VRCInstantiate(prefab);
        if (flask == null) return;

        Transform baseTf = (spawnPoint != null) ? spawnPoint : this.transform;
        flask.transform.SetPositionAndRotation(baseTf.position, baseTf.rotation);
        if (parentRoot != null) flask.transform.SetParent(parentRoot, true);

        if (visualController != null)
        {
            visualController.ApplyElementVisual(flask, elementId, 0.98f, 1.0f);
        }
    }
}
