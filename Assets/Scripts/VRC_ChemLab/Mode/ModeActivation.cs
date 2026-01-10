using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/ModeActivation")]
public class ModeActivation : UdonSharpBehaviour
{
    [Header("PCモードで ON / OFF")]
    public GameObject[] pcOn, pcOff;

    [Header("VRモードで ON / OFF")]
    public GameObject[] vrOn, vrOff;

    [Header("切替時通知（OnModePC / OnModeVR を実装しているU#へ送信）")]
    public UdonSharpBehaviour[] notifyOnPC, notifyOnVR;

    [Header("Router")]
    public ModeRouter router;
    public bool applyOnEnable = true;

    // --- Re-entrancy / recursion guards (Udon runtime safety) ---
    private bool _isApplying;
    private bool _hasApplied;
    private bool _lastIsVR;

    private void OnEnable()
    {
        // Register first (router may immediately broadcast mode)
        if (router != null)
        {
            router.Register(this);

            if (applyOnEnable)
            {
                SafeApply(router.IsVR());
            }
        }
        else if (applyOnEnable)
        {
            ApplyStandalone();
        }
    }

    public void ApplyModeFromRouter(ModeRouter r, bool isVR)
    {
        router = r;
        SafeApply(isVR);
    }

    public void ApplyStandalone()
    {
        var lp = Networking.LocalPlayer;
        bool isVR = (lp != null && lp.IsUserInVR());
        SafeApply(isVR);
    }

    private void SafeApply(bool isVR)
    {
        // Prevent stack overflow caused by SetActive -> OnEnable cascades / router rebroadcast loops
        if (_isApplying) return;

        // Idempotent: same mode already applied => do nothing
        if (_hasApplied && _lastIsVR == isVR) return;

        _isApplying = true;
        _hasApplied = true;
        _lastIsVR = isVR;

        ApplyInternal(isVR);

        _isApplying = false;
    }

    private void ApplyInternal(bool isVR)
    {
        Set(pcOn, !isVR); Set(pcOff, isVR);
        Set(vrOn, isVR); Set(vrOff, !isVR);

        if (isVR) Send(notifyOnVR, "OnModeVR");
        else Send(notifyOnPC, "OnModePC");
    }

    private void Set(GameObject[] a, bool v)
    {
        if (a == null) return;

        for (int i = 0; i < a.Length; i++)
        {
            var go = a[i];
            if (go == null) continue;

            // Avoid toggling the same GameObject that hosts this behaviour (can cause recursive enable/disable storms)
            if (go == gameObject) continue;

            // Skip if already in desired state (reduces enable cascades)
            if (go.activeSelf == v) continue;

            go.SetActive(v);
        }
    }

    private void Send(UdonSharpBehaviour[] a, string ev)
    {
        if (a == null) return;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != null) a[i].SendCustomEvent(ev);
    }
}
