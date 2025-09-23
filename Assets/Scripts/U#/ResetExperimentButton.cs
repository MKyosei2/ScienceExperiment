using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// 実験リセットボタン。物理的に押すと Orchestrator の ResetExperiment() を呼ぶ。
/// </summary>
public class ResetExperimentButton : UdonSharpBehaviour
{
    [Header("▼ Orchestrator参照")]
    public ExperimentOrchestrator orchestrator;

    public override void Interact()
    {
        if (orchestrator != null)
        {
            Debug.Log("[ResetExperimentButton] ResetExperiment triggered");
            orchestrator.ResetExperiment();
        }
    }
}
