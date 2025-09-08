using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/AIRequestSender")]
public class AIRequestSender : UdonSharpBehaviour
{
    public void Run(SelectedObjectHolder holder, ExperimentOrchestrator orchestrator)
    {
        string payload = holder != null ? holder.ToString() : "(no payload)";
        if (orchestrator != null) orchestrator.OnAIVisual("AI mock: " + payload);
    }
}
