using UdonSharp;
using UnityEngine;

public class ResetExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner elementSpawner;

    public void Press()
    {
        if (elementSpawner != null)
            elementSpawner.ResetExperiment();
    }
}
