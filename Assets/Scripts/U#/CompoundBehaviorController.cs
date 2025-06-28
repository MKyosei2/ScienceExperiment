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

        GravityMode gravity = environmentController.currentGravity;
        Hemisphere region = environmentController.currentHemisphere;

        if (gravity == GravityMode.Normal)
        {
            targetBody.useGravity = true;
        }
        else if (gravity == GravityMode.ZeroG)
        {
            targetBody.useGravity = false;
            targetBody.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }

        if (region == Hemisphere.South)
        {
            visualObject.transform.Rotate(Vector3.up * 180);
        }
        else if (region == Hemisphere.Equator)
        {
            Renderer renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }
        }
    }
}