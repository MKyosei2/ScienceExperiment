using UnityEngine;

public class SelectionActionController : MonoBehaviour
{
    public enum ActionMode { SpawnFixedPrefab, SpawnFromSelector, SelectFromZone }

    [Header("Mode")]
    [SerializeField] private ActionMode mode = ActionMode.SpawnFromSelector;

    [Header("Refs")]
    [SerializeField] private GenericSelector selector;
    [SerializeField] private Transform zone;
    [SerializeField] private GameObject fixedPrefab;
    [SerializeField] private SelectedObjectHolder selected;

    [Header("Options")]
    [SerializeField] private bool replaceExisting = true;

    public void Execute()
    {
        switch (mode)
        {
            case ActionMode.SpawnFixedPrefab:
                SpawnFixed();
                break;
            case ActionMode.SpawnFromSelector:
                if (selector) selector.SpawnOrReplace();
                else Debug.LogWarning("[SelectionActionController] Selector not set.");
                break;
            case ActionMode.SelectFromZone:
                SelectFromZone();
                break;
        }
    }

    private void SpawnFixed()
    {
        if (!fixedPrefab || !zone) { Debug.LogWarning("[SelectionActionController] fixedPrefab or zone missing."); return; }
        if (replaceExisting)
        {
            for (int i = zone.childCount - 1; i >= 0; i--) Destroy(zone.GetChild(i).gameObject);
        }
        var go = Instantiate(fixedPrefab, zone.position, zone.rotation, zone);
        if (selector) selector.TrySetSelection(go); else selected?.SetAny(go);
    }

    private void SelectFromZone()
    {
        if (!zone || !selected) { Debug.LogWarning("[SelectionActionController] zone or selected missing."); return; }
        if (zone.childCount == 0) return;
        var go = zone.GetChild(0).gameObject;
        if (selector) selector.TrySetSelection(go); else selected.SetAny(go);
    }
}
