using UdonSharp;
using UnityEngine;

// トップレベル列挙体（U#は入れ子NG）
public enum SelectionActionMode
{
    SpawnFixedPrefab = 0,
    SpawnFromButton = 1,   // ← SpawnSelectorButton を押す
    SelectFromZone = 2
}

public class SelectionActionController : UdonSharpBehaviour
{
    [Header("Mode")]
    public SelectionActionMode mode = SelectionActionMode.SpawnFromButton;

    [Header("Refs")]
    public SpawnSelectorButton spawnButton; // ← GenericSelector から置換
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
        else if (mode == SelectionActionMode.SpawnFromButton)
        {
            if (spawnButton != null) spawnButton.Press();
            else Debug.LogWarning("[SelectionActionController] spawnButton not set.");
        }
        else // SelectFromZone
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
        if (selected != null) selected.SetAny(go);
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
        selected.SetAny(go);
    }
}
