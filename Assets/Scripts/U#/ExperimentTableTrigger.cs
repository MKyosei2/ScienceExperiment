using UdonSharp;
using UnityEngine;

public class ExperimentTableTrigger : UdonSharpBehaviour
{
    public Transform tableRoot;
    public SelectedObjectHolder holder;

    private void OnTriggerEnter(Collider other)
    {
        var placeable = other.GetComponent<PlaceableObject>();
        if (placeable != null && placeable.isFixed)
        {
            string objName = other.gameObject.name.ToLower();
            if (objName.Contains("element")) holder.AddElement(other.gameObject.name);
            else if (objName.Contains("tool")) holder.AddTool(other.gameObject.name);
        }
    }
}
