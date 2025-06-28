using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DualTriggerController : UdonSharpBehaviour
{
    public ExperimentController controller;

    public override void Interact()
    {
        string tag = null;
        if (Networking.LocalPlayer != null)
        {
            tag = Networking.LocalPlayer.GetPlayerTag("CurrentRoom");
        }
        if (tag == "ExperimentRoom")
        {
            if (controller != null)
            {
                controller.RunExperiment();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Networking.LocalPlayer.IsUserInVR()) return;

        if (collision.collider != null && collision.collider.gameObject.name == "ExperimentTool")
        {
            string tag = null;
            if (Networking.LocalPlayer != null)
            {
                tag = Networking.LocalPlayer.GetPlayerTag("CurrentRoom");
            }
            if (tag == "ExperimentRoom" && controller != null)
            {
                controller.RunExperiment();
            }
        }
    }
}