using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementSymbolExporter : MonoBehaviour
{
    public ElementData[] sourceElements;
    public ElementLoader targetLoader;

    void Awake()
    {
        string[] symbols = new string[sourceElements.Length];
        for (int i = 0; i < sourceElements.Length; i++)
        {
            symbols[i] = sourceElements[i] != null ? sourceElements[i].symbol : "";
        }

        targetLoader.symbols = symbols;
        targetLoader.elementCount = symbols.Length;
    }
}
