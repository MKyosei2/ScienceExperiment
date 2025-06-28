using UdonSharp;
using UnityEngine;

public class ElementLoader : UdonSharpBehaviour
{
    public string[] symbols;
    public int elementCount;

    public string GetSymbol(int index)
    {
        if (index >= 0 && index < symbols.Length)
        {
            return symbols[index];
        }
        return "";
    }
}
