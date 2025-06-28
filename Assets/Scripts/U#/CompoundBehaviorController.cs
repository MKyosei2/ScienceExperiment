using UdonSharp;
using UnityEngine;

public class CompoundBehaviorController : UdonSharpBehaviour
{
    public Rigidbody targetBody;
    public GameObject visualObject;
    public EnvironmentController environmentController;

    void Start()
    {
        ApplyBehavior();
    }

    public void ApplyBehavior()
    {
        if (targetBody == null || environmentController == null) return;

        var gravity = environmentController.currentGravity;
        var region = environmentController.currentHemisphere;

        switch (gravity)
        {
            case EnvironmentController.GravityMode.Normal:
                targetBody.useGravity = true;
                break;

            case EnvironmentController.GravityMode.ZeroG:
                targetBody.useGravity = false;
                targetBody.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
                break;
        }

        if (region == EnvironmentController.Hemisphere.South)
        {
            visualObject.transform.Rotate(Vector3.up * 180);
        }
        else if (region == EnvironmentController.Hemisphere.Equator)
        {
            visualObject.GetComponent<Renderer>().material.color = Color.yellow;
        }
    }
}
