using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemElementDatabase
/// parallel arrays driven by Importer.
/// UdonSharp compatible (no Dictionary).
/// </summary>
public class ChemElementDatabase : UdonSharpBehaviour
{
    [Header("Core Element Data (parallel arrays)")]
    public string[] Symbols;         // "H" "He" ...
    public Color[] DisplayColors;    // 見た目色
    public float[] MeltingPointC;    // ℃（未定義は +Infinity 推奨）
    public float[] BoilingPointC;    // ℃（未定義は +Infinity 推奨）
    public int[] HazardFlags;        // 任意（ビットフラグ）

    // HazardFlags (optional)
    public const int HAZ_FLAMMABLE = 1 << 0;
    public const int HAZ_TOXIC = 1 << 1;
    public const int HAZ_CORROSIVE = 1 << 2;
    public const int HAZ_OXIDIZER = 1 << 3;
    public const int HAZ_WATER_REACTIVE = 1 << 4;
    public const int HAZ_RADIOACTIVE = 1 << 5;

    [Header("Extended (optional)")]
    public int[] AtomicNumbers;
    public string[] NamesJa;
    public string[] NamesEn;

    [Header("Chemistry (optional)")]
    [Tooltip("Common ionic charge for simple formula building. Example: Na=+1, Ca=+2, Al=+3, O=-2, Cl=-1")]
    public int[] CommonCharges;

    private int IndexOf(string symbol)
    {
        if (Symbols == null || symbol == null) return -1;
        for (int i = 0; i < Symbols.Length; i++)
        {
            if (Symbols[i] == symbol) return i;
        }
        return -1;
    }

    public bool ContainsSymbol(string symbol)
    {
        return IndexOf(symbol) >= 0;
    }

    public Color GetColor(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || DisplayColors == null || i >= DisplayColors.Length) return Color.white;
        return DisplayColors[i];
    }

    public float GetMP(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || MeltingPointC == null || i >= MeltingPointC.Length) return float.PositiveInfinity;
        return MeltingPointC[i];
    }

    public float GetBP(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || BoilingPointC == null || i >= BoilingPointC.Length) return float.PositiveInfinity;
        return BoilingPointC[i];
    }

    public int GetHazard(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || HazardFlags == null || i >= HazardFlags.Length) return 0;
        return HazardFlags[i];
    }

    public string GetNameJa(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || NamesJa == null || i >= NamesJa.Length) return symbol;
        string n = NamesJa[i];
        return string.IsNullOrEmpty(n) ? symbol : n;
    }

    public string GetNameEn(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || NamesEn == null || i >= NamesEn.Length) return symbol;
        string n = NamesEn[i];
        return string.IsNullOrEmpty(n) ? symbol : n;
    }

    /// <summary>
    /// Return a typical ionic charge for the given element symbol.
    /// Used by ReactionPredictor.BuildBinaryIonicFormula.
    /// If unknown, returns 0.
    /// </summary>
    public int GetCommonCharge(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || CommonCharges == null || i >= CommonCharges.Length) return 0;
        return CommonCharges[i];
    }
}
