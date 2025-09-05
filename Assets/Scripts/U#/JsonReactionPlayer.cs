using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class JsonReactionPlayer : UdonSharpBehaviour
{
    public Text output; // 任意

    public void Play(string json)
    {
        if (output) output.text = "JSON受信: " + json;
        Debug.Log("[JsonReaction] " + json);
        // 実際の演出に合わせてパース処理を追加してください
    }
}
