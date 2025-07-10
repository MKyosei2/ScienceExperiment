using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SelectorObject : UdonSharpBehaviour
{
    public string objectType;  // "Element", "Tool", "Condition"
    public string objectID;    // e.g., "Na", "beaker", "air_normal"

    public void Select(SelectedObjectHolder holder)
    {
        if (objectType == "Element") holder.selectedElementID = objectID;
        else if (objectType == "Tool") holder.selectedToolID = objectID;
        else if (objectType == "Condition") holder.selectedConditionID = objectID;
    }
}