using UdonSharp;
using UnityEngine;

public class ChemElementDatabase : UdonSharpBehaviour
{
    [Header("Elements")]
    public string[] Symbols;          // 元素記号
    public string[] Groups;           // 反応グループ
    public Color[] Colors;            // 色
    public float[] MeltingPoints;     // 融点
    public float[] BoilingPoints;     // 沸点

    // ===============================
    // Index を取得
    // ===============================
    private int IndexOf(string symbol)
    {
        for (int i = 0; i < Symbols.Length; i++)
        {
            if (Symbols[i] == symbol)
                return i;
        }
        return -1;
    }

    // ===============================
    // 色
    // ===============================
    public Color GetColor(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return Color.white;
        return Colors[i];
    }

    // ===============================
    // 融点
    // ===============================
    public float GetMeltingPoint(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return 0f;
        return MeltingPoints[i];
    }

    // ===============================
    // 沸点
    // ===============================
    public float GetBoilingPoint(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return 100f;
        return BoilingPoints[i];
    }

    // ===============================
    // グループ
    // ===============================
    public string GetGroup(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0) return "";
        return Groups[i];
    }

    // ===============================
    // Spawner 用（必要なら）
    // ===============================
    public bool TryGetIndex(string symbol, out int index)
    {
        index = IndexOf(symbol);
        return (index >= 0);
    }
}
