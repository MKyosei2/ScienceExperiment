using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FlaskBehaviour : UdonSharpBehaviour
{
    protected bool isActive = false; // Update実行フラグ

    // 元素が適用された瞬間に呼ばれる
    public virtual void OnElementApplied(int elementId)
    {
        // VRモードなら即アクティブ化
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR())
        {
            isActive = true;
        }
    }

    // PCモードで実験開始ボタンから呼ばれる
    public void Activate()
    {
        isActive = true;
    }

    // 子クラスで常時処理を実装
    public virtual void Update()
    {
        if (!isActive) return;
    }
}
