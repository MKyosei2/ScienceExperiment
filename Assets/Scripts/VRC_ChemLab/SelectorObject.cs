using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/SelectorObject")]
public class SelectorObject : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public SelectionCategory category = SelectionCategory.Element;
    public string idOverride = "";

    public bool parentToZoneOnSelect = false;
    public Transform zoneForThisCategory;

    public override void Interact() { Select(); }

    public void Select()
    {
        if (selected == null) return;
        if (parentToZoneOnSelect && zoneForThisCategory != null)
        {
            transform.SetParent(zoneForThisCategory, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        selected.AddSelection(category, gameObject, string.IsNullOrEmpty(idOverride) ? gameObject.name : idOverride);
    }
}
