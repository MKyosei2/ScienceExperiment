using UdonSharp;
using UnityEngine;

public class GenericSelector : UdonSharpBehaviour
{
    [Header("Config")]
    public ESelectorCategory category = ESelectorCategory.Element;
    public GameObject prefab;
    public Transform zone;
    public bool replaceExisting = true;

    [Header("Optional")]
    public SelectedObjectHolder selected;

    public GameObject SpawnOrReplace()
    {
        if (prefab == null || zone == null)
        {
            Debug.LogWarning("[GenericSelector] Prefab/Zone not set");
            return null;
        }

        if (replaceExisting)
        {
            for (int i = zone.childCount - 1; i >= 0; i--)
                GameObject.Destroy(zone.GetChild(i).gameObject);
        }

        var go = GameObject.Instantiate(prefab, zone.position, zone.rotation, zone);
        go.name = category.ToString() + "-" + prefab.name;
        TrySetSelection(go);
        return go;
    }

    public bool TrySetSelection(GameObject go)
    {
        if (go == null || selected == null) return false;

        if (category == ESelectorCategory.Element) selected.SetElement(go);
        else if (category == ESelectorCategory.Tool) selected.SetTool(go);
        else selected.SetCondition(go);

        return true;
    }
}
