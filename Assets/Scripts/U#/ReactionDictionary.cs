using UdonSharp;
using UnityEngine;

public class ReactionDictionary : UdonSharpBehaviour
{
    [TextArea] public string[] dictionaryEntries;

    public string GenerateReactionKey(string[] elements, string env)
    {
        System.Array.Sort(elements);
        return string.Join(",", elements) + "|" + env;
    }

    public bool ContainsReaction(string key)
    {
        foreach (string line in dictionaryEntries)
            if (line.StartsWith(key + "=")) return true;
        return false;
    }

    public string GetReactionName(string key)
    {
        foreach (string line in dictionaryEntries)
        {
            if (line.StartsWith(key + "="))
            {
                string[] parts = line.Split('=');
                return parts.Length > 1 ? parts[1].Split('|')[0] : "???";
            }
        }
        return "???";
    }

    public int GetVisualStyle(string key)
    {
        foreach (string line in dictionaryEntries)
        {
            if (line.StartsWith(key + "="))
            {
                string[] parts = line.Split('=');
                if (parts.Length > 1 && parts[1].Contains("|"))
                {
                    string stylePart = parts[1].Split('|')[1];
                    int style;
                    if (int.TryParse(stylePart, out style)) return style;
                }
            }
        }
        return 0;
    }
}
