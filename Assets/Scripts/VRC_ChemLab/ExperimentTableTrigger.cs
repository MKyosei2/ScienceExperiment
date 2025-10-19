using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/ExperimentTableTrigger")]
public class ExperimentTableTrigger : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public Transform zoneForThisCategory;

    private void OnTriggerEnter(Collider other)
    {
        if (zoneForThisCategory != null) other.transform.SetParent(zoneForThisCategory, true);
    }
}
