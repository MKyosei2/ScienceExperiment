using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/StartExperimentButton")]
public class StartExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public override void Interact()
    {
        _OnClick();
    }

    public void _OnClick()
    {
        if (spawner != null)
        {
            spawner.SendCustomEvent("_StartExperiment");
        }
        else
        {
            Debug.LogWarning("[StartExperimentButton] spawner 未設定");
        }
    }
}
