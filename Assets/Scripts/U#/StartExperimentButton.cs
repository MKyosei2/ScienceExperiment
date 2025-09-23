using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// 実験開始ボタン。物理的に押すと Orchestrator の StartExperiment() を呼ぶ。
/// </summary>
public class StartExperimentButton : UdonSharpBehaviour
{
    [Header("▼ Orchestrator参照")]
    public ExperimentOrchestrator orchestrator;

    public override void Interact()
    {
        if (orchestrator != null)
        {
            Debug.Log("[StartExperimentButton] StartExperiment triggered");
            orchestrator.StartExperiment();
        }
    }
}
