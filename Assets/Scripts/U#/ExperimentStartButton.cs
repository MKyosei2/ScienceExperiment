using UdonSharp;
using UnityEngine;

public class ExperimentStartButton : UdonSharpBehaviour
{
    public ExperimentController controller;
    public ModeSwitcher modeSwitcher;
    public Transform experimentTableRoot;

    public override void Interact()
    {
        if (!modeSwitcher.IsVRMode())
        {
            controller.CollectFromTable(experimentTableRoot);
            controller.RunExperiment();
        }
    }
}
