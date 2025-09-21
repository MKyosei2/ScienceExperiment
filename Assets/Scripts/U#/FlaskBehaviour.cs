using UdonSharp;
using UnityEngine;

public class FlaskBehaviour : UdonSharpBehaviour
{
    // 元素を適用した瞬間に呼ばれる
    public virtual void OnElementApplied(int elementId) { }

    // 実験中に常時動作させたい処理を子クラスで実装
    public virtual void Update() { }
}
