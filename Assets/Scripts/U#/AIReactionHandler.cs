using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class AIReactionHandler : UdonSharpBehaviour
{
    public CompoundSpawner spawner;
    public Text resultText;
    public AudioSource audioSource;
    public AudioClip successClip;

    public void HandleResponse(string json)
    {
        string reaction = Extract(json, "\"reaction\"");
        string funFact = Extract(json, "\"funFact\"");
        string styleStr = Extract(json, "\"style\"");
        int.TryParse(styleStr, out int style);

        if (spawner != null) spawner.SpawnCompound(reaction, style);
        if (resultText != null) resultText.text = reaction + "\n" + funFact;
        if (audioSource != null && successClip != null) audioSource.PlayOneShot(successClip);
    }

    private string Extract(string json, string key)
    {
        int start = json.IndexOf(key);
        if (start == -1) return "";
        start = json.IndexOf(":", start) + 1;
        if (json[start] == '\"') start++;
        int end = json.IndexOf('\"', start);
        return json.Substring(start, end - start);
    }
}