using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/ModeActivation")]
public class ModeActivation : UdonSharpBehaviour
{
    [Header("PCモードで ON / OFF にするオブジェクト")]
    public GameObject[] pcOn;
    public GameObject[] pcOff;

    [Header("VRモードで ON / OFF にするオブジェクト")]
    public GameObject[] vrOn;
    public GameObject[] vrOff;

    [Header("（任意）切替時に呼ぶ Udon イベント")]
    public UdonSharpBehaviour[] notifyOnPC;   // OnModePC()
    public UdonSharpBehaviour[] notifyOnVR;   // OnModeVR()

    [Header("Router (Scene)")]
    public ModeRouter router;                 // 空でOK（スポーン時に注入）

    public bool applyOnEnable = true;

    private void OnEnable()
    {
        if (router != null)
        {
            router.Register(this);
            if (applyOnEnable) ApplyModeFromRouter(router, router.IsVR());
        }
        else
        {
            if (applyOnEnable) ApplyStandalone();
        }
    }

    public void ApplyModeFromRouter(ModeRouter r, bool isVR)
    {
        router = r;
        Apply(isVR);
    }

    public void ApplyStandalone()
    {
        var lp = Networking.LocalPlayer;
        bool isVR = lp != null && lp.IsUserInVR();
        Apply(isVR);
    }

    private void Apply(bool isVR)
    {
        SetActiveArray(pcOn, !isVR);
        SetActiveArray(pcOff, isVR);
        SetActiveArray(vrOn, isVR);
        SetActiveArray(vrOff, !isVR);

        if (isVR) SendEvents(notifyOnVR, "OnModeVR");
        else SendEvents(notifyOnPC, "OnModePC");
    }

    private void SetActiveArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            var go = arr[i];
            if (go != null) go.SetActive(state);
        }
    }

    private void SendEvents(UdonSharpBehaviour[] arr, string ev)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            var t = arr[i];
            if (t != null) t.SendCustomEvent(ev);
        }
    }
}
