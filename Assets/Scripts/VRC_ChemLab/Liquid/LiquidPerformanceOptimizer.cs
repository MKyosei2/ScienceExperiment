using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LiquidPerformanceOptimizer : UdonSharpBehaviour
{
    public ParticleSystem particle;
    public float disableDistance = 6f;

    private VRCPlayerApi local;

    private void Start()
    {
        local = Networking.LocalPlayer;
    }

    private void Update()
    {
        if (local == null || particle == null) return;

        Vector3 head = local.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

        float dist = Vector3.Distance(transform.position, head);

        var emission = particle.emission;
        emission.enabled = (dist < disableDistance);
    }
}
