using UdonSharp;
using UnityEngine;

public class StartExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public override void Interact()
    {
        if (spawner != null)
        {
            Debug.Log("[StartExperimentButton] Experiment Started!");
            spawner._StartExperiment();
        }
        else
        {
            Debug.LogWarning("[StartExperimentButton] Spawner not assigned!");
        }
    }
}
