using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LiquidPerformanceOptimizer : UdonSharpBehaviour
{
    public ParticleSystem liquid;
    public float maxDistance = 8f;

    private VRCPlayerApi localPlayer;
    private Vector3 camPos;

    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }

    private void Update()
    {
        if (localPlayer == null || liquid == null) return;

        var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        camPos = head.position;

        float dist = Vector3.Distance(transform.position, camPos);

        var emission = liquid.emission;
        if (dist > maxDistance)
        {
            emission.rateOverTime = 5f;   // 遠く → 超軽量
        }
        else
        {
            emission.rateOverTime = 40f;  // 近く → 高品質
        }
    }
}
