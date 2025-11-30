using UdonSharp;
using UnityEngine;

public class ChemElementDatabase : UdonSharpBehaviour
{
    [Header("118 Element Symbols")]
    public string[] symbols = new string[118];

    [Header("118 Element Colors (Base Color)")]
    public Color32[] colors = new Color32[118];

    public readonly string[] AllElementSymbols = new string[]
{
    "H","He","Li","Be","B","C","N","O","F","Ne",
    "Na","Mg","Al","Si","P","S","Cl","Ar",
    "K","Ca","Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
    "Ga","Ge","As","Se","Br","Kr",
    "Rb","Sr","Y","Zr","Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd",
    "In","Sn","Sb","Te","I","Xe",
    "Cs","Ba","La","Ce","Pr","Nd","Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er","Tm","Yb","Lu",
    "Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg",
    "Tl","Pb","Bi","Po","At","Rn",
    "Fr","Ra","Ac","Th","Pa","U","Np","Pu","Am","Cm","Bk","Cf","Es","Fm","Md","No","Lr",
    "Rf","Db","Sg","Bh","Hs","Mt","Ds","Rg","Cn","Nh","Fl","Mc","Lv","Ts","Og"
};

    public string[] GetAllElements()
    {
        return AllElementSymbols;
    }

    [Header("118 Element Groups")]
    /*
      0 = その他
      1 = アルカリ金属
      2 = アルカリ土類金属
      3 = 遷移金属
      4 = ハロゲン
      5 = 希ガス
      6 = 非金属
    */
    public int[] groups = new int[118];

    [Header("Element Density")]
    public float[] density = new float[118];

    [Header("Element Viscosity")]
    public float[] viscosity = new float[118];

    [Header("Element Luminescence")]
    public float[] luminescence = new float[118];

    // --- API ---
    public int GetIndex(string symbol)
    {
        for (int i = 0; i < symbols.Length; i++)
            if (symbols[i] == symbol) return i;

        return -1;
    }

    public Color32 GetColor(string symbol)
    {
        int i = GetIndex(symbol);
        if (i < 0) return new Color32(180, 180, 180, 255);
        return colors[i];
    }

    public int GetGroup(string symbol)
    {
        int i = GetIndex(symbol);
        if (i < 0) return 0;
        return groups[i];
    }

    public float GetDensity(string symbol)
    {
        int i = GetIndex(symbol);
        if (i < 0) return 1f;
        return density[i];
    }

    public float GetViscosity(string symbol)
    {
        int i = GetIndex(symbol);
        if (i < 0) return 0.5f;
        return viscosity[i];
    }

    public float GetLuminescence(string symbol)
    {
        int i = GetIndex(symbol);
        if (i < 0) return 0f;
        return luminescence[i];
    }
}
