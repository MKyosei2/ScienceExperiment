// Assets/Scripts/U#/StartExperimentButton.cs
using UdonSharp;
using UnityEngine;

public class StartExperimentButton : UdonSharpBehaviour
{
    public ExperimentOrchestrator orchestrator;

    public override void Interact()
    {
        if (orchestrator != null) orchestrator.StartExperiment();
    }

    public void Press() { Interact(); }
}
