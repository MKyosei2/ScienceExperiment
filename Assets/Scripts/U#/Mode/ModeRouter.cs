using UdonSharp;
using UnityEngine;

public class ModeRouter : UdonSharpBehaviour
{
    private bool _isVR = false;

    // 旧コード互換: 引数つき Register を残す
    public void Register(object target = null)
    {
        Debug.Log("[ModeRouter] Register called (target=" + (target != null ? target.ToString() : "null") + ")");
    }

    // ★メソッド形式の判定だけを提供（プロパティは置かない）
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
