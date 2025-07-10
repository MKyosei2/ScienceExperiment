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
        string id = holder.selectedElementID;
        int index = GetIndex(id);
        if (index != -1)
        {
            GameObject obj = VRCInstantiate(elementPrefabs[index]);
            obj.transform.position = spawnPoint.position;
            Renderer r = obj.GetComponent<Renderer>();
        }
    }

    private int GetIndex(string id)
    {
        for (int i = 0; i < elementIDs.Length; i++) if (elementIDs[i] == id) return i;
        return -1;
    }
}