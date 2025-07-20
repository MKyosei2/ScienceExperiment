using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneSelectionButton : UdonSharpBehaviour
{
    public SelectionZone selectionZone;
    public SelectedObjectHolder holder;

    public override void Interact()
    {
        GameObject obj = selectionZone.GetFirstObject();
        if (obj == null) return;

        SelectorObject selector = obj.GetComponent<SelectorObject>();
        if (selector != null)
        {
            selector.Select(holder);
        }
    }
}