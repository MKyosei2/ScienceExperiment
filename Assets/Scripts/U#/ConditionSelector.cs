using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ConditionSelector : UdonSharpBehaviour
{
    public string conditionId;
    public GameObject managerObject;

    public void SelectCondition()
    {
        if (managerObject != null)
        {
            var holder = managerObject.GetComponent<UdonSharpBehaviour>();
            if (holder != null)
                holder.SendCustomEvent($"OnConditionSelected_{conditionId}");
        }
    }
}
