using UdonSharp;
using UnityEngine;

public class ChemElementDatabase : UdonSharpBehaviour
{
    [Header("Core Element Data (parallel arrays)")]
    public string[] Symbols;         // "H" "He" ...
    public Color[] DisplayColors;    // 見た目色
    public float[] MeltingPointC;    // ℃（未定義は +Infinity 推奨）
    public float[] BoilingPointC;    // ℃（未定義は +Infinity 推奨）
    public int[] HazardFlags;        // 任意（ビットフラグ）

    // HazardFlags
    public const int HAZ_FLAMMABLE = 1 << 0;
    public const int HAZ_TOXIC = 1 << 1;
    public const int HAZ_CORROSIVE = 1 << 2;
    public const int HAZ_OXIDIZER = 1 << 3;
    public const int HAZ_WATER_REACTIVE = 1 << 4;
    public const int HAZ_RADIOACTIVE = 1 << 5;

    // ===== 強化：教材＆見た目＆AI用の拡張データ（任意）=====
    [Header("Extended Element Data (optional, parallel arrays)")]
    public int[] AtomicNumbers;            // 例: H=1
    public string[] NamesJa;               // 例: "水素"
    public string[] NamesEn;               // 例: "Hydrogen"
    public int[] GroupNumbers;             // 1..18（不明0）
    public int[] PeriodNumbers;            // 1..7（不明0）
    public int[] CategoryIds;              // 自由にID定義
    public float[] AtomicMass;             // u（不明0）
    public float[] Density_g_cm3;          // g/cm^3（不明0）
    public float[] Electronegativity;      // Pauling（不明は -1 推奨）
    public int[] CommonIonCharge;          // 例: Na=+1, Mg=+2, O=-2, Cl=-1（不明0）

    private int IndexOf(string symbol)
    {
        if (Symbols == null) return -1;
        for (int i = 0; i < Symbols.Length; i++)
        {
            if (Symbols[i] == symbol) return i;
        }
        return -1;
    }

    // ---- Core getters ----
    public Color GetElementColor(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || DisplayColors == null) return Color.white;
        return DisplayColors[i];
    }

    public float GetMP(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || MeltingPointC == null) return float.PositiveInfinity;
        return MeltingPointC[i];
    }

    public float GetBP(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || BoilingPointC == null) return float.PositiveInfinity;
        return BoilingPointC[i];
    }

    public int GetHazard(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || HazardFlags == null) return 0;
        return HazardFlags[i];
    }

    // ---- Extended getters（未設定は安全側）----
    public int GetAtomicNumber(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || AtomicNumbers == null) return 0;
        return AtomicNumbers[i];
    }

    public string GetNameJa(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || NamesJa == null) return "";
        return NamesJa[i];
    }

    public float GetDensity(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || Density_g_cm3 == null) return 0f;
        return Density_g_cm3[i];
    }

    public int GetCommonCharge(string symbol)
    {
        int i = IndexOf(symbol);
        if (i < 0 || CommonIonCharge == null) return 0;
        return CommonIonCharge[i];
    }

    // --- 式パース（括弧なし：H2O, Fe2O3, NaCl 等）---
    // Udon向け：固定長バッファ渡し、戻り値は使用数
    public int ParseFormulaNoParens(string formula, string[] symBuf, int[] cntBuf)
    {
        if (string.IsNullOrEmpty(formula) || symBuf == null || cntBuf == null) return 0;

        for (int k = 0; k < symBuf.Length; k++) { symBuf[k] = ""; cntBuf[k] = 0; }

        int used = 0;
        int i = 0;
        while (i < formula.Length)
        {
            char c = formula[i];
            if (c < 'A' || c > 'Z') { i++; continue; }

            // symbol: Upper + optional lower
            string sym = "" + c;
            int j = i + 1;
            if (j < formula.Length)
            {
                char c2 = formula[j];
                if (c2 >= 'a' && c2 <= 'z')
                {
                    sym += c2;
                    j++;
                }
            }

            // digits
            int count = 0;
            while (j < formula.Length)
            {
                char d = formula[j];
                if (d < '0' || d > '9') break;
                count = count * 10 + (d - '0');
                j++;
            }
            if (count <= 0) count = 1;

            // merge
            int idx = -1;
            for (int t = 0; t < used; t++)
            {
                if (symBuf[t] == sym) { idx = t; break; }
            }

            if (idx >= 0) cntBuf[idx] += count;
            else if (used < symBuf.Length)
            {
                symBuf[used] = sym;
                cntBuf[used] = count;
                used++;
            }

            i = j;
        }

        return used;
    }

    // ---- 式→色（教育用の簡易合成：加重平均）----
    public Color GetColorFromFormula(string formula)
    {
        string[] s = new string[16];
        int[] n = new int[16];
        int used = ParseFormulaNoParens(formula, s, n);
        if (used <= 0) return Color.white;

        float total = 0f;
        float r = 0f, g = 0f, b = 0f;
        for (int i = 0; i < used; i++)
        {
            if (string.IsNullOrEmpty(s[i]) || n[i] <= 0) continue;
            Color c = GetElementColor(s[i]);
            float w = n[i];
            total += w;
            r += c.r * w;
            g += c.g * w;
            b += c.b * w;
        }
        if (total <= 0f) return Color.white;
        return new Color(r / total, g / total, b / total, 1f);
    }

    // ---- 式→状態（教育用の簡易推定：MP/BP加重平均）----
    public ElementState GetStateFromFormulaAtTemp(string formula, float tempC)
    {
        string[] s = new string[16];
        int[] n = new int[16];
        int used = ParseFormulaNoParens(formula, s, n);
        if (used <= 0) return ElementState.Solid;

        float total = 0f;
        float mp = 0f;
        float bp = 0f;

        for (int i = 0; i < used; i++)
        {
            if (string.IsNullOrEmpty(s[i]) || n[i] <= 0) continue;
            float w = n[i];
            total += w;
            mp += GetMP(s[i]) * w;
            bp += GetBP(s[i]) * w;
        }

        if (total <= 0f) return ElementState.Solid;

        mp /= total;
        bp /= total;

        if (tempC < mp) return ElementState.Solid;
        if (tempC < bp) return ElementState.Liquid;
        return ElementState.Gas;
    }

    // ===== 強化：式→危険フラグ合成（AI強化で使用）=====
    public int GetHazardFromFormula(string formula)
    {
        int flags = 0;
        string[] s = new string[16];
        int[] n = new int[16];
        int used = ParseFormulaNoParens(formula, s, n);
        for (int i = 0; i < used; i++)
        {
            if (string.IsNullOrEmpty(s[i])) continue;
            flags |= GetHazard(s[i]);
        }
        return flags;
    }

    // ---- Safety hint（具体手順は出さず注意喚起だけ）----
    public string BuildSafetyHintFromFormula(string formula, string reactionTag)
    {
        int flags = GetHazardFromFormula(formula);

        // 反応タグによる補足
        if (reactionTag == "oxidation") flags |= HAZ_OXIDIZER;

        string msg = "";
        if ((flags & HAZ_TOXIC) != 0) msg += "有害の可能性：吸い込み/接触に注意。 ";
        if ((flags & HAZ_CORROSIVE) != 0) msg += "腐食性の可能性：皮膚・目の保護に注意。 ";
        if ((flags & HAZ_FLAMMABLE) != 0) msg += "可燃性の可能性：火気や高温に注意。 ";
        if ((flags & HAZ_OXIDIZER) != 0) msg += "酸化性の可能性：周囲の素材に注意。 ";
        if ((flags & HAZ_WATER_REACTIVE) != 0) msg += "水反応性の可能性：取り扱いに注意。 ";
        if ((flags & HAZ_RADIOACTIVE) != 0) msg += "放射性の可能性：距離と遮蔽に注意。 ";

        if (string.IsNullOrEmpty(msg)) msg = "安全：観察を中心に、ワールド内の安全演出に従ってください。";
        return msg.Trim();
    }
}
