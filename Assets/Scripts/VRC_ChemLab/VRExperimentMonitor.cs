using UdonSharp;
using UnityEngine;

public class VRExperimentMonitor : UdonSharpBehaviour
{
    public void Log(string message)
    {
        Debug.Log("[Monitor] " + message);
    }
}
