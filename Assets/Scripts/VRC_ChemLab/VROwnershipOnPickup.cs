using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/VR/Ownership On Pickup")]
public class VROwnershipOnPickup : UdonSharpBehaviour
{
    [Header("Optional: also take ownership of these objects")]
    public GameObject[] alsoTakeOwnership;

    [Header("Optional: claim operator early (recommended for low-latency feel)")]
    public ChemElementSpawner spawner;

    public override void OnPickup()
    {
        var lp = Networking.LocalPlayer;
        if (lp == null) return;

        // Take ownership of this pickup object
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(lp, gameObject);
        }

        // Optionally take ownership of related objects (VFX, child props, etc.)
        if (alsoTakeOwnership != null)
        {
            for (int i = 0; i < alsoTakeOwnership.Length; i++)
            {
                var go = alsoTakeOwnership[i];
                if (go == null) continue;
                if (!Networking.IsOwner(go))
                {
                    Networking.SetOwner(lp, go);
                }
            }
        }

        // If spawner exists, "touching" the tool claims operator via SetOps01()
        // (SetOps01 internally calls EnsureCanControl/ClaimOperator in your project design)
        if (spawner != null)
        {
            spawner.SetOps01(
                spawner.GetHeat01(),
                spawner.GetStir01(),
                spawner.GetPour01(),
                spawner.GetShake01()
            );
        }
    }
}
