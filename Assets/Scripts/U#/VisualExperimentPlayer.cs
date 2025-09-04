using UnityEngine;

public class VisualExperimentPlayer : MonoBehaviour
{
    public void PlayStart(SelectedObjectHolder sel) { Debug.Log("[Visual] Start"); }
    public void PlayMessage(string message) { Debug.Log("[Visual] " + message); }
    public void PlayFallback() { Debug.Log("[Visual] Fallback"); }
}
