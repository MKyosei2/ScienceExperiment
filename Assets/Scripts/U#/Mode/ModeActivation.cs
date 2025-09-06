// Assets/Scripts/U#/Mode/ModeActivation.cs
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/Mode/ModeActivation")]
public class ModeActivation : UdonSharpBehaviour
{
    [Header("PCモード時に ON / OFF にするオブジェクト")]
    public GameObject[] pcOn;
    public GameObject[] pcOff;

    [Header("VRモード時に ON / OFF にするオブジェクト")]
    public GameObject[] vrOn;
    public GameObject[] vrOff;

    [Header("開発用: 強制モード（本番は両方 false 推奨）")]
    public bool forcePC = false;
    public bool forceVR = false;

    [Tooltip("生成直後や有効化時に自動適用する")]
    public bool applyOnEnable = true;

    private void OnEnable() { if (applyOnEnable) ApplyMode(); }

    /// Spawner等から明示的に呼びたいとき
    public void ApplyMode()
    {
        bool isVR = ResolveIsVR();
        if (isVR)
        {
            SetActiveArray(vrOn, true);
            SetActiveArray(vrOff, false);
            SetActiveArray(pcOn, false);
            SetActiveArray(pcOff, true);
        }
        else
        {
            SetActiveArray(pcOn, true);
            SetActiveArray(pcOff, false);
            SetActiveArray(vrOn, false);
            SetActiveArray(vrOff, true);
        }
    }

    private bool ResolveIsVR()
    {
        if (forcePC) return false;
        if (forceVR) return true;
        var lp = Networking.LocalPlayer;
        return lp != null && lp.IsUserInVR();
    }

    private void SetActiveArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null) arr[i].SetActive(state);
        }
    }
}
