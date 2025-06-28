using UdonSharp;
using UnityEngine;

public class ReactionDictionary : UdonSharpBehaviour
{
    [TextArea] public string[] dictionaryEntries;

    public string GenerateReactionKey(string[] elements, string env)
    {
        // Udon does not support System.Array.Sort, so use a simple bubble sort
        for (int i = 0; i < elements.Length - 1; i++)
        {
            for (int j = 0; j < elements.Length - i - 1; j++)
            {
                if (string.Compare(elements[j], elements[j + 1]) > 0)
                {
                    string temp = elements[j];
                    elements[j] = elements[j + 1];
                    elements[j + 1] = temp;
                }
            }
        }
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
