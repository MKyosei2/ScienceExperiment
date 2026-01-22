using UdonSharp;
using UnityEngine;

public class StartExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public override void Interact()
    {
        if (spawner == null)
        {
            GameObject g = GameObject.Find("ChemElementSpawner");
            if (g != null) spawner = g.GetComponent<ChemElementSpawner>();
        }

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
