using UnityEngine;

public class VRExperimentMonitor : MonoBehaviour
{
    public void Log(string msg) { Debug.Log("[Monitor] " + msg); }
    public void LogWarning(string msg) { Debug.LogWarning("[Monitor] " + msg); }
    public void LogError(string msg) { Debug.LogError("[Monitor] " + msg); }
}
