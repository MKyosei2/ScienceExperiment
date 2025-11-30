// Assets/Scripts/U#/Mode/PCOnly.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/PCOnly")]
public class PCOnly : UdonSharpBehaviour
{
    [Tooltip("空ならこのコンポーネントが付いている GameObject 自体を切り替え")]
    public GameObject[] targets;

    [Header("開発用: 強制モード（本番は両方 false 推奨）")]
    public bool forcePC = false;
    public bool forceVR = false;

    private void OnEnable() { Apply(); }
    public void Apply()
    {
        bool isVR = ResolveIsVR();
        bool activeInPC = !isVR; // PCOnly → PCでtrue, VRでfalse
        SetActive(activeInPC);
    }

    private bool ResolveIsVR()
    {
        if (forcePC) return false;
        if (forceVR) return true;
        var lp = Networking.LocalPlayer;
        return lp != null && lp.IsUserInVR();
    }

    private void SetActive(bool state)
    {
        if (targets == null || targets.Length == 0)
        {
            gameObject.SetActive(state);
            return;
        }
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null) targets[i].SetActive(state);
        }
    }
}
