using UdonSharp;
using UnityEngine;

public class ElementLoader : UdonSharpBehaviour
{
    public ElementData[] elements;
    public string[] symbols;
    public int elementCount => elements.Length;

    void Start()
    {
        symbols = new string[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            symbols[i] = elements[i].symbol;
        }
    }

    public ElementData GetElement(int index)
    {
        return (index >= 0 && index < elements.Length) ? elements[index] : null;
    }
}
