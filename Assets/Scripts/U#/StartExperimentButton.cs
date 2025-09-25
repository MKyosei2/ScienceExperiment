using UdonSharp;
using UnityEngine;

public class StartExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner elementSpawner;

    public void Press()
    {
        if (elementSpawner != null)
            elementSpawner.StartExperiment();
    }
}
