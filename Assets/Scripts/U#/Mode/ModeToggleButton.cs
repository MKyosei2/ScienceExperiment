using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// PCモード／VRモード切替ボタン。物理的に押すと Orchestrator の ToggleMode() を呼ぶ。
/// </summary>
public class ModeToggleButton : UdonSharpBehaviour
{
    [Header("▼ Orchestrator参照")]
    public ExperimentOrchestrator orchestrator;

    public override void Interact()
    {
        if (orchestrator != null)
        {
            orchestrator.ToggleMode();
            Debug.Log("[ModeToggleButton] Mode changed: " + (orchestrator.isPCMode ? "PC Mode" : "VR Mode"));
        }
    }
}
