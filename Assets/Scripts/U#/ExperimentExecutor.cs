using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentExecutor : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public string[] elementIDs;
    public GameObject[] elementPrefabs;
    public Transform spawnPoint;

    public override void Interact()
    {
        if (holder.selectedElementIDs.Length == 0) return;
        string id = holder.selectedElementIDs[0];
        int index = GetIndex(id);
        if (index != -1 && spawnPoint != null)
        {
            GameObject prefab = elementPrefabs[index];
            GameObject obj = VRCInstantiate(prefab);
            obj.transform.position = spawnPoint.position;
        }
    }

    private int GetIndex(string id)
    {
        for (int i = 0; i < elementIDs.Length; i++)
        {
            if (elementIDs[i] == id) return i;
        }
        return -1;
    }
}
