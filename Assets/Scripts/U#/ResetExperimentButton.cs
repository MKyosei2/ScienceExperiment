// Assets/Scripts/U#/ResetExperimentButton.cs
using UdonSharp;
using UnityEngine;

public class ResetExperimentButton : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public Transform elementZone;
    public Transform toolZone;
    public Transform conditionZone;

    public override void Interact()
    {
        ResetAll();
    }

    public void Press() { ResetAll(); }

    private void ResetAll()
    {
        ClearChildren(elementZone);
        ClearChildren(toolZone);
        ClearChildren(conditionZone);

        if (selected != null) selected.ClearAll();
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(t.GetChild(i).gameObject);
        }
    }
}
