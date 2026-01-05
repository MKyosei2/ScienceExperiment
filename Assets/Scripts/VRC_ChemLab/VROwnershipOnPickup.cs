using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/VR/Ownership On Pickup")]
public class VROwnershipOnPickup : UdonSharpBehaviour
{
    [Header("Optional: also take ownership of these objects")]
    public GameObject[] alsoTakeOwnership;

    [Header("Optional: claim operator early (no UI change)")]
    public ChemElementSpawner spawner;

    public override void OnPickup()
    {
        var lp = Networking.LocalPlayer;
        if (lp == null) return;

        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(lp, gameObject);

        if (alsoTakeOwnership != null)
        {
            for (int i = 0; i < alsoTakeOwnership.Length; i++)
            {
                var go = alsoTakeOwnership[i];
                if (go != null && !Networking.IsOwner(go))
                    Networking.SetOwner(lp, go);
            }
        }

        // 触った瞬間に操作者を確定させたい（遅延/競合減）
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
