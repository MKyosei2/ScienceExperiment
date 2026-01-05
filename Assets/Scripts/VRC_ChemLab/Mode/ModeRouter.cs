using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/ModeRouter")]
public class ModeRouter : UdonSharpBehaviour
{
    [Header("When true, even VR users are treated as PC mode")]
    public bool forcePC = false;

    [Header("Capacity")]
    public int maxActivations = 32;

    private ModeActivation[] _acts;
    private int _count;

    private void Start()
    {
        if (_acts == null || _acts.Length == 0)
            _acts = new ModeActivation[Mathf.Max(4, maxActivations)];
    }

    public bool IsVR()
    {
        if (forcePC) return false;

        var lp = Networking.LocalPlayer;
        if (lp == null) return false;
        return lp.IsUserInVR();
    }

    // ModeActivation.OnEnable() から呼ばれる
    public void Register(ModeActivation target)
    {
        if (target == null) return;

        if (_acts == null || _acts.Length == 0)
            _acts = new ModeActivation[Mathf.Max(4, maxActivations)];

        // already registered?
        for (int i = 0; i < _count; i++)
        {
            if (_acts[i] == target) return;
        }

        if (_count >= _acts.Length)
        {
            Debug.Log("[ModeRouter] Register overflow. Increase maxActivations.");
            return;
        }

        _acts[_count++] = target;
    }

    public void Toggle()
    {
        forcePC = !forcePC;
        ApplyAll();
        Debug.Log("[ModeRouter] Mode: " + (IsVR() ? "VR" : "PC"));
    }

    public void ApplyAll()
    {
        bool isVR = IsVR();
        for (int i = 0; i < _count; i++)
        {
            var a = _acts[i];
            if (a != null)
                a.ApplyModeFromRouter(this, isVR);
        }
    }
}
