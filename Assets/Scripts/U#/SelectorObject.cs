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

        switch (objectType)
        {
            case "Element": holder.selectedElementID = objectID; break;
            case "Tool": holder.selectedToolID = objectID; break;
            case "Condition": holder.selectedConditionID = objectID; break;
        }
    }

    public void Select(SelectedObjectHolder targetHolder)
    {
        if (targetHolder == null) return;

        switch (objectType)
        {
            case "Element": targetHolder.selectedElementID = objectID; break;
            case "Tool": targetHolder.selectedToolID = objectID; break;
            case "Condition": targetHolder.selectedConditionID = objectID; break;
        }
    }

    public override void Interact()
    {
        Select(); // VR/PC 共通でクリック選択
    }
}
