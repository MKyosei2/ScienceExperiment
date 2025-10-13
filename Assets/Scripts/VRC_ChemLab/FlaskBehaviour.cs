using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class FlaskBehaviour : UdonSharpBehaviour
{
    protected bool isActive = false;

    // 元素が適用された瞬間に呼ばれる
    public virtual void OnElementApplied(int elementId)
    {
        if (Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR())
        {
            isActive = true; // VRは即開始
        }
    }

    // PCモードで共通ボタンから有効化
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
