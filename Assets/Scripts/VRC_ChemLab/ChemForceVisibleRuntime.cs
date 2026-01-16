using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VRC_ChemLab
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ChemForceVisibleRuntime : UdonSharpBehaviour
    {
        [Header("Targets (optional; auto-found by name if null)")]
        public Transform beakerRoot;          // Beaker_Pickup
        public Transform sampleVisualRoot;    // SampleVisual
        public Transform reactionVfxRoot;     // ReactionVFX (optional)

        [Header("Auto-find names")]
        public string beakerName = "Beaker_Pickup";
        public string sampleVisualName = "SampleVisual";
        public string reactionVfxName = "ReactionVFX";

        [Header("Placement")]
        public Transform beakerAnchor; // VR_StartZone
        public Vector3 beakerWorldOffset = new Vector3(0f, 0.02f, 0f);
        public bool forceBeakerToAnchor = true;

        [Header("Debug placement (optional)")]
        public bool placeBeakerInFrontOfPlayer = false;
        public float playerForwardMeters = 1.2f;
        public float playerUpMeters = 0.6f;

        [Header("Scale safety")]
        public bool detachBeakerToWorld = true;
        public Vector3 forcedWorldScale = new Vector3(1f, 1f, 1f);

        [Header("Visibility forcing")]
        public int forcedLayer = 0; // Default
        public bool forceLayerRecursively = true;
        public bool forceRenderersEnabled = true;
        public bool forceParticlesPlay = true;

        [Header("Maintenance")]
        public float refreshInterval = 0.5f;

        private float _nextRefresh;

        private void Start()
        {
            AutoFindIfNeeded();
            ApplyAll();
            _nextRefresh = Time.time + Mathf.Max(0.2f, refreshInterval);
        }

        private void Update()
        {
            if (Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + Mathf.Max(0.2f, refreshInterval);

            AutoFindIfNeeded();
            ApplyAll();
        }

        private void AutoFindIfNeeded()
        {
            if (beakerRoot == null)
            {
                GameObject go = GameObject.Find(beakerName);
                if (go != null) beakerRoot = go.transform;
            }

            if (sampleVisualRoot == null)
            {
                GameObject go = GameObject.Find(sampleVisualName);
                if (go != null) sampleVisualRoot = go.transform;
            }

            if (reactionVfxRoot == null)
            {
                GameObject go = GameObject.Find(reactionVfxName);
                if (go != null) reactionVfxRoot = go.transform;
            }

            if (beakerAnchor == null)
            {
                GameObject go = GameObject.Find("VR_StartZone");
                if (go != null) beakerAnchor = go.transform;
            }
        }

        private void ApplyAll()
        {
            if (beakerRoot != null)
            {
                ForceActiveChain(beakerRoot);

                if (detachBeakerToWorld)
                {
                    beakerRoot.SetParent(null, true);
                    beakerRoot.localScale = forcedWorldScale;
                }

                if (placeBeakerInFrontOfPlayer)
                {
                    VRCPlayerApi lp = Networking.LocalPlayer;
                    if (lp != null)
                    {
                        Vector3 pos = lp.GetPosition();
                        Quaternion rot = lp.GetRotation();
                        beakerRoot.position = pos + (rot * Vector3.forward) * playerForwardMeters + Vector3.up * playerUpMeters;
                        beakerRoot.rotation = rot;
                    }
                }
                else if (forceBeakerToAnchor && beakerAnchor != null)
                {
                    beakerRoot.position = beakerAnchor.position + beakerWorldOffset;
                    beakerRoot.rotation = beakerAnchor.rotation;
                }

                ForceVisible(beakerRoot);
            }

            if (sampleVisualRoot != null)
            {
                ForceActiveChain(sampleVisualRoot);
                ForceVisible(sampleVisualRoot);
            }

            if (reactionVfxRoot != null)
            {
                ForceActiveChain(reactionVfxRoot);
                ForceVisible(reactionVfxRoot);
            }
        }

        private void ForceActiveChain(Transform t)
        {
            Transform cur = t;
            int guard = 0;
            while (cur != null && guard < 64)
            {
                if (!cur.gameObject.activeSelf) cur.gameObject.SetActive(true);
                cur = cur.parent;
                guard++;
            }
        }

        private void ForceVisible(Transform root)
        {
            if (root == null) return;

            if (forceLayerRecursively)
            {
                Transform[] trs = root.GetComponentsInChildren<Transform>();
                for (int i = 0; i < trs.Length; i++)
                {
                    Transform t = trs[i];
                    if (t == null) continue;
                    t.gameObject.layer = forcedLayer;
                }
            }
            else
            {
                root.gameObject.layer = forcedLayer;
            }

            if (forceRenderersEnabled)
            {
                Renderer[] rs = root.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rs.Length; i++)
                {
                    Renderer r = rs[i];
                    if (r == null) continue;
                    r.enabled = true;
                }
            }

            if (forceParticlesPlay)
            {
                ParticleSystem[] ps = root.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < ps.Length; i++)
                {
                    ParticleSystem p = ps[i];
                    if (p == null) continue;
                    var em = p.emission;
                    em.enabled = true;
                    if (!p.isPlaying) p.Play(true);
                }
            }
        }
    }
}
