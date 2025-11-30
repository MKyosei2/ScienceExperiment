using UdonSharp;
using UnityEngine;
using TMPro;

[AddComponentMenu("VRC Lab/JsonReactionPlayer")]
public class JsonReactionPlayer : UdonSharpBehaviour
{
    public TextMeshProUGUI output;

    public void Play(string json)
    {
        if (output != null)
        {
            output.text = json;
            Debug.Log($"[JsonReactionPlayer] 再生: {json}");
        }
    }
}
