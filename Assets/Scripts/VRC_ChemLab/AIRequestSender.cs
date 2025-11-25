using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/AIRequestSender")]
public class AIRequestSender : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public void ReceiveAIResponse(string bondInfo)
    {
        if (spawner == null)
        {
            Debug.LogWarning("[AIRequestSender] ChemElementSpawner 未設定");
            return;
        }

        spawner.SendCustomEvent("_ApplyBondUpdate");
        Debug.Log("[AIRequestSender] AI反応データをSpawnerへ送信");
    }
}
