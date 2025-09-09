using UdonSharp;
using UnityEngine;
using TMPro;

[AddComponentMenu("VRC Lab/ExperimentOrchestrator")]
public class ExperimentOrchestrator : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public AIRequestSender ai;
    public VisualExperimentPlayer visualPlayer;
    public JsonReactionPlayer jsonPlayer;
    public TextMeshProUGUI status;

    public void StartExperiment()
    {
        if (selected == null) { SetStatus("No SelectedHolder."); return; }
        if (!selected.IsValid()) { SetStatus("Select >=2 Elements, >=1 Tool, 1 Condition."); return; }

        SetStatus("Running...");
        if (ai != null) ai.Run(selected, this);
        else if (visualPlayer != null) visualPlayer.Play("Experiment running (mock)...");
    }

    public void OnAIVisual(string text) { if (visualPlayer != null) visualPlayer.Play(text); SetStatus("Done."); }
    public void OnAIJson(string json) { if (jsonPlayer != null) jsonPlayer.Play(json); SetStatus("Done."); }

    public void ResetAll() { if (selected != null) selected.ClearAll(); SetStatus("Ready."); }
    private void SetStatus(string s) { if (status != null) status.text = s; }
}
