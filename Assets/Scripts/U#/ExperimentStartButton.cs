using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ExperimentStartButton : UdonSharpBehaviour
{
    public ExperimentController controller;
    public ModeSwitcher modeSwitcher;

    public override void Interact()
    {
        if (modeSwitcher != null && !modeSwitcher.IsVRMode())
        {
            controller.RunExperiment();
        }
        else
        {
            Debug.Log("🔒 VRモードでは実験ボタン無効");
        }
    }
}
