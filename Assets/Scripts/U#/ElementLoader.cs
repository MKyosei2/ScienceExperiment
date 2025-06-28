using UdonSharp;
using UnityEngine;

public class ElementLoader : UdonSharpBehaviour
{
    public ElementData[] elements;
    public string[] symbols;
    public int elementCount;

    void Start()
    {
        int len = elements.Length;
        symbols = new string[len];
        for (int i = 0; i < len; i++)
        {
            symbols[i] = "";
            ElementData e = elements[i];
            if (e != null)
            {
                symbols[i] = e.cachedSymbol; // cachedSymbol を使うことで UdonSharp の制限を回避
            }
        }

        elementCount = len;
    }

    public ElementData GetElement(int index)
    {
        if (index >= 0 && index < elements.Length)
        {
            return elements[index];
        }
        return null;
    }
}