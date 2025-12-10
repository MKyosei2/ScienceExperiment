using UdonSharp;
using UnityEngine;

public class AIRequestSender : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public void ReceiveAIResponse(string text)
    {
        Debug.Log("[AI] Response received: " + text);
        spawner._ApplyBondUpdate();
    }
}
