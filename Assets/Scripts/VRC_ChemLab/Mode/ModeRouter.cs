using UdonSharp;
using UnityEngine;

public class ModeRouter : UdonSharpBehaviour
{
    private bool _isVR = false;

    public void Register(object target = null)
    {
        Debug.Log("[ModeRouter] Register called (target=" + (target != null ? target.ToString() : "null") + ")");
    }

    // 旧コードが IsVR() とメソッド呼出するためこちらだけ提供（プロパティは置かない）
    public bool IsVR()
    {
        return _isVR;
    }

    public void Toggle()
    {
        _isVR = !_isVR;
        Debug.Log("[ModeRouter] モード切替: " + (_isVR ? "VR" : "PC"));
    }
}
