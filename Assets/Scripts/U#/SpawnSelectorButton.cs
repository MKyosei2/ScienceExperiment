using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/Spawn/SpawnSelectorButton")]
public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("What to spawn")] public GameObject prefab;
    [Header("Where to put it")] public Transform zone;

    [Header("Selection")] public SelectedObjectHolder selected;
    public SelectionCategory category = SelectionCategory.Element;

    [Header("Mode")] public ModeRouter router;
    public bool replaceExisting = false;

    public override void Interact() { Spawn(); }
    public void Press() { Spawn(); }

    public void Spawn()
    {
        if (prefab == null || zone == null || selected == null) return;

        if (replaceExisting)
            for (int i = zone.childCount - 1; i >= 0; i--) GameObject.Destroy(zone.GetChild(i).gameObject);

        GameObject go = GameObject.Instantiate(prefab);
        go.transform.SetParent(zone, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var mas = go.GetComponentsInChildren<ModeActivation>(true);
        for (int i = 0; i < mas.Length; i++)
        {
            var m = mas[i];
            if (router != null) { router.Register(m); m.ApplyModeFromRouter(router, router.IsVR()); }
            else { m.ApplyStandalone(); }
        }

        selected.AddSelection(category, go, go.name);
    }
}
