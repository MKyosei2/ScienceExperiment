using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/StartExperimentButton")]
public class StartExperimentButton : UdonSharpBehaviour
{
    public ExperimentOrchestrator orchestrator;
    public override void Interact() { if (orchestrator != null) orchestrator.StartExperiment(); }
}
