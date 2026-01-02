using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemElementDatabase
/// parallel arrays driven by Importer.
/// UdonSharp compatible (no Dictionary).
///
/// 2026-01 update:
/// - Optional chemistry hints (AtomicMass/Electronegativity/IsMetal/TypicalValence)
///   for local "generative" visuals.
/// - Optional Known Compounds tables (Inspector入力) for "正解"物性/見た目.
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

    [Header("Chemistry Hints for Generative Visuals (optional)")]
    [Tooltip("Approx atomic mass (g/mol). Used for local inference only.")]
    public float[] AtomicMass;

    [Tooltip("Electronegativity (Pauling). Used for local inference only.")]
    public float[] Electronegativity;

    [Tooltip("True if element is treated as metal in visuals.")]
    public bool[] IsMetal;

    [Tooltip("Typical valence hint (+/-). Optional.")]
    public int[] TypicalValence;

    // ===============================
    // Known Compounds (Inspector入力)
    // ===============================
    [Header("Known Compounds (optional, parallel arrays)")]
    [Tooltip("e.g. H2O, NaCl, CO2 ... Exact match after NormalizeFormula().")]
    public string[] CompoundFormulas;
    public string[] CompoundNamesJa;
    public Color[] CompoundDisplayColors;
    public float[] CompoundMeltingPointC;
    public float[] CompoundBoilingPointC;
    public int[] CompoundHazardFlags;

    [Header("Known Compound Visual Recipe (optional)")]
    [Tooltip("0=Crystal,1=Powder,2=Metal,3=Liquid,4=GasFog")]
    public int[] CompoundArchetype;
    [Tooltip("0=None,1=Glint,2=Precipitate,3=Bubble,4=Fog")]
    public int[] CompoundParticlePreset;

    [Range(0f, 1f)] public float[] CompoundOpacity;
    [Range(0f, 1f)] public float[] CompoundMetallic;
    [Range(0f, 1f)] public float[] CompoundSmoothness;
    [Range(0f, 5f)] public float[] CompoundEmission;
    [Range(0f, 5f)] public float[] CompoundNoiseScale;
    [Range(0f, 2f)] public float[] CompoundFogDensity;
    [Range(0f, 2f)] public float[] CompoundBubbleRate;
    [Range(0f, 2f)] public float[] CompoundViscosity;
    [Range(0f, 2f)] public float[] CompoundDensity;

    // =====================================================
    // Element helpers
    // =====================================================
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

    // Optional hints
    public float GetAtomicMass(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || AtomicMass == null || i >= AtomicMass.Length) return 0f;
        return AtomicMass[i];
    }

    public float GetElectronegativity(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || Electronegativity == null || i >= Electronegativity.Length) return 0f;
        return Electronegativity[i];
    }

    public bool GetIsMetal(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || IsMetal == null || i >= IsMetal.Length) return false;
        return IsMetal[i];
    }

    public int GetTypicalValence(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || TypicalValence == null || i >= TypicalValence.Length) return 0;
        return TypicalValence[i];
    }

    // =====================================================
    // Known Compounds helpers
    // =====================================================

    /// <summary>
    /// Normalizes a formula string for table matching.
    /// - Trim
    /// - Remove spaces
    /// - Replace '·' with '.'
    /// </summary>
    public string NormalizeFormula(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return "";
        string f = formula.Trim();
        // remove spaces
        f = f.Replace(" ", "");
        // unify dot
        f = f.Replace("·", ".");
        return f;
    }

    private int IndexOfCompound(string formula)
    {
        if (CompoundFormulas == null || formula == null) return -1;
        string f = NormalizeFormula(formula);
        int len = CompoundFormulas.Length;
        for (int i = 0; i < len; i++)
        {
            string s = CompoundFormulas[i];
            if (string.IsNullOrEmpty(s)) continue;
            if (NormalizeFormula(s) == f) return i;
        }
        return -1;
    }

    public bool ContainsCompound(string formula)
    {
        return IndexOfCompound(formula) >= 0;
    }

    public string GetCompoundNameJa(string formula)
    {
        int i = IndexOfCompound(formula);
        if (i < 0 || CompoundNamesJa == null || i >= CompoundNamesJa.Length) return formula;
        string n = CompoundNamesJa[i];
        return string.IsNullOrEmpty(n) ? formula : n;
    }

    public Color GetCompoundColor(string formula)
    {
        int i = IndexOfCompound(formula);
        if (i < 0 || CompoundDisplayColors == null || i >= CompoundDisplayColors.Length) return Color.white;
        return CompoundDisplayColors[i];
    }

    public float GetCompoundMP(string formula)
    {
        int i = IndexOfCompound(formula);
        if (i < 0 || CompoundMeltingPointC == null || i >= CompoundMeltingPointC.Length) return float.PositiveInfinity;
        return CompoundMeltingPointC[i];
    }

    public float GetCompoundBP(string formula)
    {
        int i = IndexOfCompound(formula);
        if (i < 0 || CompoundBoilingPointC == null || i >= CompoundBoilingPointC.Length) return float.PositiveInfinity;
        return CompoundBoilingPointC[i];
    }

    public int GetCompoundHazard(string formula)
    {
        int i = IndexOfCompound(formula);
        if (i < 0 || CompoundHazardFlags == null || i >= CompoundHazardFlags.Length) return 0;
        return CompoundHazardFlags[i];
    }

    /// <summary>
    /// Compound Visual Recipe.
    /// Returns true if formula exists in known compound table.
    /// Any missing arrays fall back to defaults.
    /// </summary>
    public bool TryGetCompoundRecipe(
        string formula,
        out Color baseColor,
        out float mpC,
        out float bpC,
        out int hazard,
        out int archetype,
        out int particlePreset,
        out float opacity,
        out float metallic,
        out float smoothness,
        out float emission,
        out float noiseScale,
        out float fogDensity,
        out float bubbleRate,
        out float viscosity,
        out float density
    )
    {
        int i = IndexOfCompound(formula);
        if (i < 0)
        {
            baseColor = Color.white;
            mpC = 25f;
            bpC = 100f;
            hazard = 0;
            archetype = 0;
            particlePreset = 0;
            opacity = 1f;
            metallic = 0f;
            smoothness = 0.4f;
            emission = 0f;
            noiseScale = 0.2f;
            fogDensity = 0.6f;
            bubbleRate = 0f;
            viscosity = 1f;
            density = 1f;
            return false;
        }

        baseColor = (CompoundDisplayColors != null && i < CompoundDisplayColors.Length) ? CompoundDisplayColors[i] : Color.white;
        mpC = (CompoundMeltingPointC != null && i < CompoundMeltingPointC.Length) ? CompoundMeltingPointC[i] : 25f;
        bpC = (CompoundBoilingPointC != null && i < CompoundBoilingPointC.Length) ? CompoundBoilingPointC[i] : 100f;
        hazard = (CompoundHazardFlags != null && i < CompoundHazardFlags.Length) ? CompoundHazardFlags[i] : 0;

        archetype = (CompoundArchetype != null && i < CompoundArchetype.Length) ? CompoundArchetype[i] : 0;
        particlePreset = (CompoundParticlePreset != null && i < CompoundParticlePreset.Length) ? CompoundParticlePreset[i] : 0;

        opacity = (CompoundOpacity != null && i < CompoundOpacity.Length) ? CompoundOpacity[i] : 1f;
        metallic = (CompoundMetallic != null && i < CompoundMetallic.Length) ? CompoundMetallic[i] : 0f;
        smoothness = (CompoundSmoothness != null && i < CompoundSmoothness.Length) ? CompoundSmoothness[i] : 0.4f;
        emission = (CompoundEmission != null && i < CompoundEmission.Length) ? CompoundEmission[i] : 0f;
        noiseScale = (CompoundNoiseScale != null && i < CompoundNoiseScale.Length) ? CompoundNoiseScale[i] : 0.2f;
        fogDensity = (CompoundFogDensity != null && i < CompoundFogDensity.Length) ? CompoundFogDensity[i] : 0.6f;
        bubbleRate = (CompoundBubbleRate != null && i < CompoundBubbleRate.Length) ? CompoundBubbleRate[i] : 0f;
        viscosity = (CompoundViscosity != null && i < CompoundViscosity.Length) ? CompoundViscosity[i] : 1f;
        density = (CompoundDensity != null && i < CompoundDensity.Length) ? CompoundDensity[i] : 1f;

        return true;
    }

    /// <summary>
    /// Unified hazard lookup for element symbol OR known compound.
    /// </summary>
    public int GetHazardForFormulaOrElement(string symbolOrFormula)
    {
        if (string.IsNullOrEmpty(symbolOrFormula)) return 0;
        string s = symbolOrFormula.Trim();

        if (ContainsSymbol(s)) return GetHazard(s);
        if (ContainsCompound(s)) return GetCompoundHazard(s);

        // fallback: try extract first symbol
        if (s.Length >= 2)
        {
            string s2 = s.Substring(0, 2);
            if (ContainsSymbol(s2)) return GetHazard(s2);
        }
        string s1 = s.Substring(0, 1);
        if (ContainsSymbol(s1)) return GetHazard(s1);

        return 0;
    }
}
