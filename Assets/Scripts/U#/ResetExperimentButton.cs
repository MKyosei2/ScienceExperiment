using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/ResetExperimentButton")]
public class ResetExperimentButton : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public Transform elementZone, toolZone, conditionZone;

    public override void Interact() { ResetAll(); }

    public void ResetAll()
    {
        DestroyChildren(elementZone);
        DestroyChildren(toolZone);
        DestroyChildren(conditionZone);
        if (selected != null) selected.ClearAll();
    }

    private void DestroyChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) GameObject.Destroy(t.GetChild(i).gameObject);
    }
}
