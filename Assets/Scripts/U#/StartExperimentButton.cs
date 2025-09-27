using UdonSharp;
using UnityEngine;

public class StartExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public override void Interact() { Press(); }
    public void Press()
    {
        if (spawner != null) spawner.StartExperiment();
    }
}
