// Assets/Scripts/U#/VisualExperimentPlayer.cs
using UdonSharp;
using UnityEngine;
using TMPro;

/// テキストで実験の進行を表示する簡易プレイヤー（演出はここを差し替えて拡張）
public class VisualExperimentPlayer : UdonSharpBehaviour
{
    public TextMeshProUGUI output;

    public void PlayMessage(string message)
    {
        if (output != null) output.text = message;
        // ここでパーティクルやアニメ等を呼ぶ
    }
}
