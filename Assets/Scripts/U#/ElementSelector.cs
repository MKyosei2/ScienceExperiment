using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ElementSelector : UdonSharpBehaviour
{
    public string elementId;
    public GameObject managerObject;

    public void SelectElement()
    {
        if (managerObject != null)
        {
            var holder = managerObject.GetComponent<UdonSharpBehaviour>();
            if (holder != null)
                holder.SendCustomEvent($"OnElementSelected_{elementId}");
        }
    }
}
