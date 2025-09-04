using UnityEngine;

public class GenericSelector : MonoBehaviour
{
    public enum Category { Element, Tool, Condition }

    [Header("Config")]
    [SerializeField] private Category category = Category.Element;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform zone;
    [SerializeField] private bool replaceExisting = true;

    [Header("Optional")]
    [SerializeField] private SelectedObjectHolder selected;

    public Category CurrentCategory => category;
    public GameObject CurrentPrefab => prefab;
    public Transform TargetZone => zone;

    public void SetCategory(Category c) => category = c;
    public void SetPrefab(GameObject p) => prefab = p;
    public void SetZone(Transform z) => zone = z;

    public GameObject SpawnOrReplace()
    {
        if (!prefab || !zone) { Debug.LogWarning("[GenericSelector] Prefab or Zone not set."); return null; }
        if (replaceExisting)
        {
            for (int i = zone.childCount - 1; i >= 0; i--) Destroy(zone.GetChild(i).gameObject);
        }
        var go = Instantiate(prefab, zone.position, zone.rotation, zone);
        go.name = $"{category}-{prefab.name}";
        TrySetSelection(go);
        return go;
    }

    public bool TrySetSelection(GameObject go)
    {
        if (!go || !selected) return false;
        switch (category)
        {
            case Category.Element: selected.SetElement(go); break;
            case Category.Tool: selected.SetTool(go); break;
            case Category.Condition: selected.SetCondition(go); break;
        }
        return true;
    }
}
