using UdonSharp;
using UnityEngine;
using VRC.Udon;

[AddComponentMenu("VRC Lab/VRExperimentMonitor")]
public class VRExperimentMonitor : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public Transform leftHand;
    public Transform rightHand;
    public Transform triggerCenter;
    public Vector3 triggerSize = new Vector3(0.4f, 0.4f, 0.4f);

    private Vector3 prevL, prevR;
    private float cooldown = 0f;

    void Update()
    {
        if (spawner == null || leftHand == null || rightHand == null) return;

        if (cooldown > 0) cooldown -= Time.deltaTime;

        Vector3 dl = leftHand.position - prevL;
        Vector3 dr = rightHand.position - prevR;

        if (Mathf.Abs(dl.y) > 0.15f && Mathf.Abs(dr.y) > 0.15f && cooldown <= 0f)
        {
            if (InRange(leftHand.position) && InRange(rightHand.position))
            {
                spawner.SendCustomEvent("_StartExperiment");
                cooldown = 2f;
            }
        }

        prevL = leftHand.position;
        prevR = rightHand.position;
    }

    private bool InRange(Vector3 pos)
    {
        if (triggerCenter == null) return true;
        Vector3 c = triggerCenter.position;
        Vector3 h = triggerSize * 0.5f;
        return Mathf.Abs(pos.x - c.x) <= h.x && Mathf.Abs(pos.y - c.y) <= h.y && Mathf.Abs(pos.z - c.z) <= h.z;
    }
}
