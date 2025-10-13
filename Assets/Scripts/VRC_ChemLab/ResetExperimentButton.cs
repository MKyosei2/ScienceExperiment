using UdonSharp;
using UnityEngine;

public class ResetExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public override void Interact() { Press(); }
    public void Press()
    {
        if (spawner != null) spawner.ResetExperiment();
    }
}
