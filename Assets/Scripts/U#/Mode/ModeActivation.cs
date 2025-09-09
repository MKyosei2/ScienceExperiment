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

    private void OnEnable()
    {
        if (router != null) { router.Register(this); if (applyOnEnable) ApplyModeFromRouter(router, router.IsVR()); }
        else if (applyOnEnable) ApplyStandalone();
    }

    public void ApplyModeFromRouter(ModeRouter r, bool isVR) { router = r; Apply(isVR); }
    public void ApplyStandalone()
    {
        var lp = Networking.LocalPlayer; bool isVR = (lp != null && lp.IsUserInVR()); Apply(isVR);
    }

    private void Apply(bool isVR)
    {
        Set(pcOn, !isVR); Set(pcOff, isVR);
        Set(vrOn, isVR); Set(vrOff, !isVR);

        if (isVR) Send(notifyOnVR, "OnModeVR");
        else Send(notifyOnPC, "OnModePC");
    }

    private void Set(GameObject[] a, bool v) { if (a == null) return; for (int i = 0; i < a.Length; i++) { var go = a[i]; if (go != null) go.SetActive(v); } }
    private void Send(UdonSharpBehaviour[] a, string ev) { if (a == null) return; for (int i = 0; i < a.Length; i++) if (a[i] != null) a[i].SendCustomEvent(ev); }
}
