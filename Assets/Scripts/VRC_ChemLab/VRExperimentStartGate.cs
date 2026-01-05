using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/VR Input/VRExperimentStartGate")]
public class VRExperimentStartGate : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    [Header("What counts as 'placed'")]
    public Transform containerRoot;         // ビーカー/フラスコのルート
    public float stableSeconds = 0.35f;     // 置いてからこの秒数経ってStart
    public float cooldownSeconds = 2.0f;

    private bool active;
    private bool inZone;
    private float enteredAt;
    private float nextAllowedAt;

    public void OnModeVR() { active = true; }
    public void OnModePC() { active = false; }

    private void Update()
    {
        if (!active) return;
        if (!inZone) return;
        if (spawner == null) return;

        if (Time.time < nextAllowedAt) return;

        if (spawner.GetPhase() != 0) return;

        // “何を/何で”が決まってないと開始しない
        if (string.IsNullOrEmpty(spawner.GetInputFormula())) return;
        if (string.IsNullOrEmpty(spawner.GetLastEquipment())) return;

        if (Time.time - enteredAt < stableSeconds) return;

        spawner._StartExperiment();
        nextAllowedAt = Time.time + cooldownSeconds;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!active) return;
        if (containerRoot == null || other == null) return;

        if (other.transform == containerRoot || other.transform.IsChildOf(containerRoot))
        {
            inZone = true;
            enteredAt = Time.time;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (containerRoot == null || other == null) return;

        if (other.transform == containerRoot || other.transform.IsChildOf(containerRoot))
        {
            inZone = false;
        }
    }
}
