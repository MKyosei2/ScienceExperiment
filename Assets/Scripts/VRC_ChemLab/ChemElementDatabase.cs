using UdonSharp;
using UnityEngine;

public class ChemElementDatabase : UdonSharpBehaviour
{
    [Header("Elements")]
    public string[] Symbols;
    public string[] Groups;
    public Color[] Colors;
    public float[] MeltingPoints;
    public float[] BoilingPoints;

    private int IndexOf(string symbol)
    {
        for (int i = 0; i < Symbols.Length; i++)
            if (Symbols[i] == symbol)
                return i;
        return -1;
    }

    public Color GetColor(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return Color.white;
        return Colors[i];
    }

    public float GetMeltingPoint(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return 0f;
        return MeltingPoints[i];
    }

    public float GetBoilingPoint(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return 100f;
        return BoilingPoints[i];
    }

    public string GetGroup(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return "";
        return Groups[i];
    }
}
