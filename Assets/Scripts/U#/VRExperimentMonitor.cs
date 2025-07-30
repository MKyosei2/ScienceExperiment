using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;

public class VRExperimentMonitor : UdonSharpBehaviour
{
    public TextMeshProUGUI logText;

    private GameObject leftItem;
    private GameObject rightItem;

    private bool isVR = false;
    private VRCPlayerApi localPlayer;

    [Header("LineRenderer (手から出る線)")]
    public LineRenderer leftPointerLine;
    public LineRenderer rightPointerLine;
    public float pointerLength = 2.0f;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;

        if (localPlayer != null)
        {
            isVR = localPlayer.IsUserInVR();
        }
        else
        {
            isVR = false; // ← デフォルトはPCモードとする
        }

        Log(isVR ? "🔁 VRモード" : "🖥️ PCモード");

        if (leftPointerLine != null) leftPointerLine.enabled = false;
        if (rightPointerLine != null) rightPointerLine.enabled = false;
    }

    void Update()
    {
        if (!isVR || localPlayer == null)
        {
            if (leftPointerLine != null) leftPointerLine.enabled = false;
            if (rightPointerLine != null) rightPointerLine.enabled = false;
            return;
        }

        if (leftPointerLine != null)
        {
            VRCPlayerApi.TrackingData leftHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            Vector3 start = leftHand.position;
            Vector3 dir = leftHand.rotation * Vector3.forward;
            leftPointerLine.enabled = true;
            leftPointerLine.SetPosition(0, start);
            leftPointerLine.SetPosition(1, start + dir * pointerLength);
        }

        if (rightPointerLine != null)
        {
            VRCPlayerApi.TrackingData rightHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            Vector3 start = rightHand.position;
            Vector3 dir = rightHand.rotation * Vector3.forward;
            rightPointerLine.enabled = true;
            rightPointerLine.SetPosition(0, start);
            rightPointerLine.SetPosition(1, start + dir * pointerLength);
        }
    }

    public void Log(string message)
    {
        if (logText != null) logText.text = message;
        Debug.Log(message);
    }

    public void AssignLeft(GameObject item)
    {
        leftItem = item;
        Log($"🖐️ 左手に「{item.name}」を持たせました");
    }

    public void AssignRight(GameObject item)
    {
        rightItem = item;
        Log($"✋ 右手に「{item.name}」を持たせました");
    }

    public void ClearLeft()
    {
        if (leftItem != null)
        {
            Log($"🖐️ 左手の「{leftItem.name}」を手放しました");
            leftItem = null;
        }
    }

    public void ClearRight()
    {
        if (rightItem != null)
        {
            Log($"✋ 右手の「{rightItem.name}」を手放しました");
            rightItem = null;
        }
    }

    public void ShowHands()
    {
        string leftName = (leftItem != null) ? leftItem.name : "なし";
        string rightName = (rightItem != null) ? rightItem.name : "なし";
        Log($"🧤 手の状態 → 左: {leftName} / 右: {rightName}");
    }

    public bool IsVRMode()
    {
        return isVR;
    }
}
