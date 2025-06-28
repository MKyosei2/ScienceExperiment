using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class VoiceZoneGate : UdonSharpBehaviour
{
    public GameObject wallObject;           // 実験部屋と傍観ルームを仕切る遮音壁
    public AudioLowPassFilter audioGate;    // 傍観ルーム側スピーカー（必要に応じて）

    void Start()
    {
        if (wallObject != null)
        {
            // 傍観者側プレイヤーに対して遮音オブジェクトを透明化しない
            wallObject.layer = 0; // Default
        }

        if (audioGate != null)
        {
            // 傍観者側のオーディオにやや距離感のあるフィルタ演出
            audioGate.cutoffFrequency = 6000f;
        }
    }
}