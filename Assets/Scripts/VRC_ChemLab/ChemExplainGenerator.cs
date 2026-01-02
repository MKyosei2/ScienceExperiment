using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemExplainGenerator
/// ・UdonSharp完全対応
/// ・外部AIなし
/// ・テンプレ＋条件分岐で「推論っぽさ」を生成
///
/// 2026-01:
/// - 既知/未知（推定）を文章に明示
/// - 操作量（攪拌/加熱/注ぐ/振る）を説明に反映
/// </summary>
public class ChemExplainGenerator : UdonSharpBehaviour
{
    [Header("Verbosity")]
    [Range(0, 2)]
    public int verbosity = 1; // 0=短い / 1=標準 / 2=少し詳しい

    [Header("Optional DB (for known/unknown text)")]
    public ChemElementDatabase elementDb;

    // =====================================================
    // 外部から呼ばれる生成API（詳細）
    // =====================================================
    public void GenerateDetailed(
        string inputFormula,
        string toolId,
        string reactionTag,
        float stir01,
        float pour01,
        float heat01,
        float shake01,
        float tempC,
        float pressureKPa,
        float humidityPct,
        bool isDangerous,
        bool isKnown,
        string inferenceNote,
        out string hint,
        out string explain,
        out string safety
    )
    {
        // -----------------------
        // Hint（次の行動）
        // -----------------------
        float op = Mathf.Clamp01(0.25f * stir01 + 0.25f * pour01 + 0.35f * heat01 + 0.15f * shake01);
        if (op < 0.25f)
        {
            hint = Pick(inputFormula, toolId, "low", new string[] {
                "変化が弱いです。攪拌や加熱を増やしてみましょう。",
                "反応が進みにくい条件です。操作量を上げて比較してください。",
                "条件不足。温度や攪拌を強めると違いが出ます。"
            });
        }
        else if (op > 0.8f)
        {
            hint = Pick(inputFormula, toolId, "high", new string[] {
                "反応が活発です。色・泡・温度変化を観察しましょう。",
                "変化が大きい状態です。現象を優先して確認してください。",
                "活発反応。安全表示を確認しつつ観察を続けましょう。"
            });
        }
        else
        {
            hint = Pick(inputFormula, toolId, "mid", new string[] {
                "条件を少しずつ変えて違いを比べてみましょう。",
                "操作を1つだけ変えると原因が分かりやすいです。",
                "攪拌・加熱・注ぐ量を調整して比較してください。"
            });
        }

        // -----------------------
        // Explain（短く・観察中心）
        // -----------------------
        string knownText = isKnown ? "既知データ" : "未知（推定表示）";
        string modeJp = ModeToJP(reactionTag);
        string header = "【種別】" + knownText + "\n" +
                        "【器具】" + Safe(toolId) + " / 【モード】" + modeJp;

        string cond =
            "【条件】T=" + Mathf.RoundToInt(tempC) + "℃  P=" + Mathf.RoundToInt(pressureKPa) + "kPa  湿度=" + Mathf.RoundToInt(humidityPct) + "%\n" +
            "       攪拌=" + Pct(stir01) + " 加熱=" + Pct(heat01) + " 注ぐ=" + Pct(pour01) + " 振る=" + Pct(shake01);

        string obs = BuildObservation(reactionTag, heat01, stir01);

        if (verbosity == 0)
        {
            explain = header + "\n" + "【観察】" + obs;
            if (!string.IsNullOrEmpty(inferenceNote))
                explain += "\n" + "【推定】" + inferenceNote;
        }
        else
        {
            explain = header + "\n" + cond + "\n" + "【観察】" + obs;

            if (!string.IsNullOrEmpty(inputFormula))
            {
                explain += "\n" + "【入力】" + inputFormula;
                if (elementDb != null)
                {
                    string name = elementDb.ContainsCompound(inputFormula) ? elementDb.GetCompoundNameJa(inputFormula)
                        : (elementDb.ContainsSymbol(inputFormula) ? elementDb.GetNameJa(inputFormula) : "");
                    if (!string.IsNullOrEmpty(name) && name != inputFormula)
                        explain += "（" + name + "）";
                }
            }

            if (!string.IsNullOrEmpty(inferenceNote))
                explain += "\n" + "【推定】" + inferenceNote;

            if (verbosity >= 2)
                explain += "\n" + "【学習】操作を1つずつ変えると因果が見えます。";
        }

        // -----------------------
        // Safety（注意のみ）
        // -----------------------
        if (isDangerous)
        {
            safety = "注意：安全表示を確認し、演出に従って観察してください。";
        }
        else
        {
            safety = "安全：通常の観察条件です。";
        }
    }

    // =====================================================
    // Legacy API (spawner互換)
    // =====================================================
    public void Generate(
        string inputFormula,
        string toolId,
        float reactionPotential,
        float tempC,
        float pressureKPa,
        float humidityPct,
        bool isDangerous,
        out string hint,
        out string explain,
        out string safety
    )
    {
        // 旧呼び出しでは操作量/モードを持っていないので簡易値で補完
        bool known = false;
        if (elementDb != null)
        {
            known = elementDb.ContainsSymbol(inputFormula) || elementDb.ContainsCompound(inputFormula);
        }

        string note = known ? "" : "組成・物性ヒントから見た目を推定しています。";
        GenerateDetailed(
            inputFormula,
            toolId,
            "auto",
            0f, 0f, Mathf.Clamp01(reactionPotential), 0f,
            tempC,
            pressureKPa,
            humidityPct,
            isDangerous,
            known,
            note,
            out hint,
            out explain,
            out safety
        );
    }

    // =====================================================
    // Internal logic
    // =====================================================

    private string BuildObservation(string mode, float heat01, float stir01)
    {
        string s;

        if (mode == "oxidation")
        {
            s = "発熱・発光・火花が出やすい。";
            if (heat01 > 0.4f) s += " 加熱が強いほど反応が進みます。";
        }
        else if (mode == "chloride" || mode == "mixing")
        {
            s = "混合反応。泡・析出・色変化が起こりやすい。";
            if (stir01 > 0.3f) s += " 攪拌で変化が強く見えることがあります。";
        }
        else if (mode == "dissolve")
        {
            s = "溶解・混合。透明度や波の変化に注目。";
        }
        else
        {
            s = "目立った変化は少ない状態です。";
        }

        return s;
    }

    private string ModeToJP(string mode)
    {
        if (mode == "oxidation") return "酸化方向";
        if (mode == "chloride") return "混合反応";
        if (mode == "mixing") return "混合";
        if (mode == "dissolve") return "溶解";
        if (mode == "auto") return "自動推定";
        return "変化小";
    }

    private string Pct(float v)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";
    }

    // UdonSharp対応・安全ハッシュ
    private string Pick(string a, string b, string key, string[] arr)
    {
        if (arr == null || arr.Length == 0) return "";

        int h = SimpleHash((a == null ? "" : a) + "|" + (b == null ? "" : b) + "|" + key);
        int idx = Mathf.Abs(h) % arr.Length;
        return arr[idx];
    }

    private int SimpleHash(string s)
    {
        int h = 0;
        int len = s == null ? 0 : s.Length;
        for (int i = 0; i < len; i++)
        {
            h = (h * 31) + (int)s[i];
        }
        return h;
    }

    private string Safe(string s)
    {
        return s == null ? "" : s;
    }
}
