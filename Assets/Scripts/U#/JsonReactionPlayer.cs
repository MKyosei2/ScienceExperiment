// Assets/Scripts/U#/JsonReactionPlayer.cs
using UdonSharp;
using UnityEngine;
using TMPro;

/// JSON文字列をそのまま表示。将来はパースしてエフェクトを制御。
public class JsonReactionPlayer : UdonSharpBehaviour
{
    public TextMeshProUGUI output;

    public void PlayJson(string json)
    {
        if (output != null) output.text = "JSON:\n" + json;
        // TODO: Udonで扱いやすい形にパースして具体的なエフェクトを再生
    }
}
