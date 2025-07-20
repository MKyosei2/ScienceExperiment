using UdonSharp;
using UnityEngine;

public class CategoryDisplayManager : UdonSharpBehaviour
{
    public GameObject[] elementObjects;
    public GameObject[] toolObjects;
    public GameObject[] conditionObjects;

    public void ShowCategory(string category)
    {
        SetActiveForAll(elementObjects, category == "Element");
        SetActiveForAll(toolObjects, category == "Tool");
        SetActiveForAll(conditionObjects, category == "Condition");
    }

    private void SetActiveForAll(GameObject[] objs, bool isActive)
    {
        foreach (var obj in objs)
        {
            if (obj != null) obj.SetActive(isActive);
        }
    }
}