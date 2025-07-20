using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SelectorObject : UdonSharpBehaviour
{
    [SerializeField] private string objectType;  // "Element", "Tool", "Condition"
    [SerializeField] private string objectID;

    public SelectedObjectHolder holder;

    public string GetObjectType() => objectType;
    public string GetObjectID() => objectID;

    public void SetObjectType(string type) => objectType = type;
    public void SetObjectID(string id) => objectID = id;

    public void SetObjectTypeAndID(string type, string id)
    {
        objectType = type;
        objectID = id;
    }

    public void Select()
    {
        if (holder == null) return;
        ApplySelection(holder);
    }

    public void Select(SelectedObjectHolder targetHolder)
    {
        if (targetHolder == null) return;
        ApplySelection(targetHolder);
    }

    private void ApplySelection(SelectedObjectHolder target)
    {
        switch (objectType)
        {
            case "Element": target.AddElement(objectID); break;
            case "Tool": target.AddTool(objectID); break;
            case "Condition": target.SetCondition(objectID); break;
        }
    }

    public override void Interact()
    {
        Select();
    }
}
