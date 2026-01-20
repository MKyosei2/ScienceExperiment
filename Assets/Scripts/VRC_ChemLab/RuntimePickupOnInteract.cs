using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRC_ChemLab
{
    [AddComponentMenu("VRC ChemLab/Runtime Pickup On Interact")]
    public class RuntimePickupOnInteract : UdonSharpBehaviour
    {
        [Tooltip("If empty, auto-finds VRCPickup on Start.")]
        public VRCPickup pickup;

        private void Start()
        {
            if (pickup == null)
                pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
        }

        public override void Interact()
        {
            if (pickup == null) return;

            // Ensure local ownership to allow pickup.
            if (Networking.LocalPlayer != null)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            // Ensure pickupable (some templates may have this turned off).
            pickup.pickupable = true;

            // Do NOT call pickup.Pickup(); not available in many SDK versions.
            // Desktop users can now pick up via standard interaction.
        }
    }
}
