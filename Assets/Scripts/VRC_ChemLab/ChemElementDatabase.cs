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
    // -------------------------------------------------
    // Shared int codes (match ChemVisualController)
    // -------------------------------------------------
    // Archetype
    public const int ARCH_CRYSTAL = 0;
    public const int ARCH_POWDER = 1;
    public const int ARCH_METAL = 2;
    public const int ARCH_LIQUID = 3;
    public const int ARCH_GASFOG = 4;

    // Particle preset
    public const int PT_NONE = 0;
    public const int PT_GLINT = 1;
    public const int PT_PRECIPITATE = 2;
    public const int PT_BUBBLE = 3;
    public const int PT_FOG = 4;

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
    // Element VFX Profile (optional, parallel arrays)
    // =====================================================
    // NOTE:
    // - These arrays must be the same length/order as Symbols.
    // - They are intended to drive ONE shared visual prefab (common structure).
    // - If missing, visuals should fall back to simple defaults derived from
    //   DisplayColors / HazardFlags / IsMetal / MP/BP.

    [Header("Element VFX Profile (optional, parallel arrays)")]
    [Tooltip("0=Crystal,1=Powder,2=Metal,3=Liquid,4=GasFog (match ChemVisualController)")]
    public int[] ElementVfxArchetype;

    [Tooltip("0=None,1=Glint,2=Precipitate,3=Bubble,4=Fog (match ChemVisualController)")]
    public int[] ElementVfxParticlePreset;

    public Color[] ElementVfxBaseColor;
    public Color[] ElementVfxAccentColor;

    [Range(0f, 1f)] public float[] ElementVfxOpacity;
    [Range(0f, 1f)] public float[] ElementVfxMetallic;
    [Range(0f, 1f)] public float[] ElementVfxSmoothness;
    [Range(0f, 5f)] public float[] ElementVfxEmission;
    [Range(0f, 5f)] public float[] ElementVfxNoiseScale;
    [Range(0f, 2f)] public float[] ElementVfxFogDensity;
    [Range(0f, 2f)] public float[] ElementVfxBubbleRate;
    [Range(0f, 2f)] public float[] ElementVfxViscosity;
    [Range(0f, 2f)] public float[] ElementVfxDensity;

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

    // -------------------------------------------------
    // Symbol resolving helpers
    // -------------------------------------------------
    /// <summary>
    /// Resolve an input token (symbol / Japanese name / English name) into a symbol.
    /// Returns empty string if not found.
    /// </summary>
    public string ResolveSymbol(string token)
    {
        if (string.IsNullOrEmpty(token)) return "";
        string t = token.Trim();

        // Symbol direct
        if (ContainsSymbol(t)) return t;

        // Try case-insensitive for symbols (e.g. "na" -> "Na")
        // We keep a simple scan because UdonSharp has no Dictionary.
        if (Symbols != null)
        {
            string up = t.ToUpper();
            for (int i = 0; i < Symbols.Length; i++)
            {
                string s = Symbols[i];
                if (string.IsNullOrEmpty(s)) continue;
                if (s.ToUpper() == up) return s;
            }
        }

        // Name match (exact)
        int idx = IndexOfName(t);
        if (idx >= 0 && Symbols != null && idx < Symbols.Length) return Symbols[idx];

        return "";
    }

    /// <summary>
    /// Find an element index by Japanese/English name (exact match).
    /// Returns -1 if not found.
    /// </summary>
    public int IndexOfName(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        string t = name.Trim();

        if (NamesJa != null)
        {
            for (int i = 0; i < NamesJa.Length; i++)
            {
                string n = NamesJa[i];
                if (string.IsNullOrEmpty(n)) continue;
                if (n == t) return i;
            }
        }

        if (NamesEn != null)
        {
            string low = t.ToLower();
            for (int i = 0; i < NamesEn.Length; i++)
            {
                string n = NamesEn[i];
                if (string.IsNullOrEmpty(n)) continue;
                if (n.ToLower() == low) return i;
            }
        }

        return -1;
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
    // Element VFX profile helper
    // =====================================================

    /// <summary>
    /// Element VFX recipe.
    /// Returns true if the element exists in Symbols.
    /// Missing/short arrays will fall back to safe defaults.
    /// </summary>
    public bool TryGetElementVfxRecipe(
        string symbol,
        out Color baseColor,
        out Color accentColor,
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
        int i = IndexOf(symbol);
        if (i < 0)
        {
            baseColor = Color.white;
            accentColor = Color.white;
            archetype = ARCH_CRYSTAL;
            particlePreset = PT_NONE;
            opacity = 1f;
            metallic = 0f;
            smoothness = 0.4f;
            emission = 0f;
            noiseScale = 0.25f;
            fogDensity = 0.6f;
            bubbleRate = 0f;
            viscosity = 1f;
            density = 1f;
            return false;
        }

        // Base colors
        // -----------------------------------------------------------------
        // IMPORTANT FIX:
        // ElementVfxBaseColor is an *optional* override. In many scenes it exists
        // but entries are left at default (0,0,0,0). When we blindly prefer it,
        // every element ends up with the same "invisible black" color.
        //
        // For correct per-element visuals, we prefer DisplayColors unless the
        // override has a meaningful alpha.
        // -----------------------------------------------------------------
        bool hasOverride = false;
        Color overrideCol = Color.clear;
        if (ElementVfxBaseColor != null && i < ElementVfxBaseColor.Length)
        {
            overrideCol = ElementVfxBaseColor[i];
            if (overrideCol.a > 0.01f) hasOverride = true;
        }

        if (hasOverride)
            baseColor = overrideCol;
        else if (DisplayColors != null && i < DisplayColors.Length)
            baseColor = DisplayColors[i];
        else
            baseColor = Color.white;

        // Color alpha for VFX is controlled by "opacity" parameter later.
        // Keep the base color opaque so shaders/particles don't disappear.
        if (baseColor.a <= 0.01f) baseColor.a = 1f;

        bool isMetal = (IsMetal != null && i < IsMetal.Length) ? IsMetal[i] : false;
        int hazard = (HazardFlags != null && i < HazardFlags.Length) ? HazardFlags[i] : 0;

        // Accent color override (optional). Same rule: ignore unset (alpha=0) entries.
        if (ElementVfxAccentColor != null && i < ElementVfxAccentColor.Length && ElementVfxAccentColor[i].a > 0.01f)
            accentColor = ElementVfxAccentColor[i];
        else
            accentColor = Color.Lerp(baseColor, isMetal ? Color.gray : Color.white, isMetal ? 0.15f : 0.35f);

        if (accentColor.a <= 0.01f) accentColor.a = 1f;

        // Archetype / particle preset
        if (ElementVfxArchetype != null && i < ElementVfxArchetype.Length)
            archetype = ElementVfxArchetype[i];
        else
        {
            // Derive from MP/BP at room temp
            float mp = (MeltingPointC != null && i < MeltingPointC.Length) ? MeltingPointC[i] : float.PositiveInfinity;
            float bp = (BoilingPointC != null && i < BoilingPointC.Length) ? BoilingPointC[i] : float.PositiveInfinity;
            if (float.IsNaN(mp) || float.IsInfinity(mp)) mp = 25f;
            if (float.IsNaN(bp) || float.IsInfinity(bp)) bp = 100f;

            float room = 25f;
            if (isMetal) archetype = ARCH_METAL;
            else if (room >= bp) archetype = ARCH_GASFOG;
            else if (room >= mp) archetype = ARCH_LIQUID;
            else archetype = ARCH_CRYSTAL;
        }

        if (ElementVfxParticlePreset != null && i < ElementVfxParticlePreset.Length)
            particlePreset = ElementVfxParticlePreset[i];
        else
        {
            if (archetype == ARCH_METAL) particlePreset = PT_GLINT;
            else if (archetype == ARCH_LIQUID) particlePreset = PT_BUBBLE;
            else if (archetype == ARCH_GASFOG) particlePreset = PT_FOG;
            else particlePreset = PT_NONE;
        }

        // Numeric params (fallbacks are chosen to be visible by default)
        opacity = (ElementVfxOpacity != null && i < ElementVfxOpacity.Length)
            ? ElementVfxOpacity[i]
            : (archetype == ARCH_GASFOG ? 0.12f : (archetype == ARCH_LIQUID ? 0.22f : 0.98f));

        metallic = (ElementVfxMetallic != null && i < ElementVfxMetallic.Length)
            ? ElementVfxMetallic[i]
            : (isMetal ? 1f : 0f);

        smoothness = (ElementVfxSmoothness != null && i < ElementVfxSmoothness.Length)
            ? ElementVfxSmoothness[i]
            : (isMetal ? 0.65f : 0.40f);

        if (ElementVfxEmission != null && i < ElementVfxEmission.Length)
            emission = ElementVfxEmission[i];
        else
        {
            // Slight emission so it's never "invisible" on dark maps
            emission = 0.03f;
            if ((hazard & HAZ_RADIOACTIVE) != 0) emission = 0.35f;
            else if ((hazard & HAZ_OXIDIZER) != 0) emission = 0.18f;
            else if ((hazard & HAZ_FLAMMABLE) != 0) emission = 0.14f;
            else if ((hazard & HAZ_CORROSIVE) != 0) emission = 0.10f;
            else if ((hazard & HAZ_TOXIC) != 0) emission = 0.08f;
        }

        noiseScale = (ElementVfxNoiseScale != null && i < ElementVfxNoiseScale.Length)
            ? ElementVfxNoiseScale[i]
            : (archetype == ARCH_GASFOG ? 1.1f : (archetype == ARCH_LIQUID ? 0.45f : 0.25f));

        fogDensity = (ElementVfxFogDensity != null && i < ElementVfxFogDensity.Length)
            ? ElementVfxFogDensity[i]
            : (archetype == ARCH_GASFOG ? 1.2f : 0.6f);

        bubbleRate = (ElementVfxBubbleRate != null && i < ElementVfxBubbleRate.Length)
            ? ElementVfxBubbleRate[i]
            : (archetype == ARCH_LIQUID ? 0.25f : 0f);

        viscosity = (ElementVfxViscosity != null && i < ElementVfxViscosity.Length)
            ? ElementVfxViscosity[i]
            : (archetype == ARCH_LIQUID ? 1.0f : 1.0f);

        density = (ElementVfxDensity != null && i < ElementVfxDensity.Length)
            ? ElementVfxDensity[i]
            : 1.0f;

        // Clamp just in case
        opacity = Mathf.Clamp01(opacity);
        metallic = Mathf.Clamp01(metallic);
        smoothness = Mathf.Clamp01(smoothness);
        if (emission < 0f) emission = 0f;
        if (noiseScale < 0f) noiseScale = 0f;
        if (fogDensity < 0f) fogDensity = 0f;
        if (bubbleRate < 0f) bubbleRate = 0f;
        if (viscosity < 0f) viscosity = 0f;
        if (density < 0f) density = 0f;

        return true;
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