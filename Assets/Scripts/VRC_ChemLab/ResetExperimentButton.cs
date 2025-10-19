using UdonSharp;
using UnityEngine;
using VRC.Udon;

[AddComponentMenu("VRC Lab/ResetExperimentButton")]
public class ResetExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public void _OnClick()
    {
        if (spawner != null)
            spawner.SendCustomEvent("_ResetExperiment");
    }
}
