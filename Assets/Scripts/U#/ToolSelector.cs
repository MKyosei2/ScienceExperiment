using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ToolSelector : UdonSharpBehaviour
{
    public string toolId;
    public GameObject managerObject;

    public void SelectTool()
    {
        if (managerObject != null)
        {
            var holder = managerObject.GetComponent<UdonSharpBehaviour>();
            if (holder != null)
                holder.SendCustomEvent($"OnToolSelected_{toolId}");
        }
    }
}
