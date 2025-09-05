using UdonSharp;
using UnityEngine;

// 入れ子enumはU#非対応のため、トップレベルに列挙体を定義
public enum SelectionActionMode
{
    SpawnFixedPrefab = 0,
    SpawnFromSelector = 1,
    SelectFromZone = 2
}

public class SelectionActionController : UdonSharpBehaviour
{
    [Header("Mode")]
    public SelectionActionMode mode = SelectionActionMode.SpawnFromSelector;

    [Header("Refs")]
    public GenericSelector selector;
    public Transform zone;
    public GameObject fixedPrefab;
    public SelectedObjectHolder selected;

    [Header("Options")]
    public bool replaceExisting = true;

    public void Execute()
    {
        if (mode == SelectionActionMode.SpawnFixedPrefab)
        {
            SpawnFixed();
        }
        else if (mode == SelectionActionMode.SpawnFromSelector)
        {
            if (selector != null) selector.SpawnOrReplace();
            else Debug.LogWarning("[SelectionActionController] Selector not set.");
        }
        else // SelectionActionMode.SelectFromZone
        {
            SelectFromZone();
        }
    }

    private void SpawnFixed()
    {
        if (fixedPrefab == null || zone == null)
        {
            Debug.LogWarning("[SelectionActionController] fixedPrefab/zone missing");
            return;
        }

        if (replaceExisting)
        {
            for (int i = zone.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(zone.GetChild(i).gameObject);
            }
        }

        GameObject go = GameObject.Instantiate(fixedPrefab, zone.position, zone.rotation, zone);
        if (selector != null) selector.TrySetSelection(go);
        else if (selected != null) selected.SetAny(go);
    }

    private void SelectFromZone()
    {
        if (zone == null || selected == null)
        {
            Debug.LogWarning("[SelectionActionController] zone/selected missing");
            return;
        }
        if (zone.childCount == 0) return;

        GameObject go = zone.GetChild(0).gameObject;
        if (selector != null) selector.TrySetSelection(go);
        else selected.SetAny(go);
    }
}
