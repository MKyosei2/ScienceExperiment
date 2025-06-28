using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DualTriggerController : UdonSharpBehaviour
{
    public ExperimentController controller;

    public override void Interact()
    {
        string tag = Networking.LocalPlayer?.GetPlayerTag("CurrentRoom");
        if (tag == "ExperimentRoom")
        {
            controller?.RunExperiment();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Networking.LocalPlayer.IsUserInVR()) return;

        if (collision.collider.CompareTag("ExperimentTool"))
        {
            string tag = Networking.LocalPlayer?.GetPlayerTag("CurrentRoom");
            if (tag == "ExperimentRoom") controller?.RunExperiment();
        }
    }
}
