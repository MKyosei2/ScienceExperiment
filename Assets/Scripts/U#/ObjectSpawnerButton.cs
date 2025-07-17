using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ObjectSpawnerButton : UdonSharpBehaviour
{
    public GameObject[] elementPrefabs;
    public GameObject[] toolPrefabs;
    public GameObject[] conditionPrefabs;
    public Transform spawnPoint;
    public SelectedObjectHolder holder;
    public ModeSwitcher modeSwitcher;

    public string currentCategory = "Element";
    public int currentIndex = 0;

    public override void Interact()
    {
        GameObject prefab = null;
        string id = "";

        if (currentCategory == "Element" && currentIndex < elementPrefabs.Length)
        {
            prefab = elementPrefabs[currentIndex];
            id = prefab.name;
            holder.AddElement(id);
        }
        else if (currentCategory == "Tool" && currentIndex < toolPrefabs.Length)
        {
            prefab = toolPrefabs[currentIndex];
            id = prefab.name;
            holder.AddTool(id);
        }
        else if (currentCategory == "Condition" && currentIndex < conditionPrefabs.Length)
        {
            prefab = conditionPrefabs[currentIndex];
            id = prefab.name;
            holder.SetCondition(id);
        }

        if (prefab != null && spawnPoint != null)
        {
            GameObject instance = VRCInstantiate(prefab);
            instance.transform.position = spawnPoint.position;
        }
    }

    public void SetCategory(string category)
    {
        currentCategory = category;
        currentIndex = 0;
    }

    public void SetIndex(int index)
    {
        currentIndex = index;
    }
}
