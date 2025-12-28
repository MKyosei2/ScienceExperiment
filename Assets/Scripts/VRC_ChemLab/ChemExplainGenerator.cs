using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemExplainGenerator
/// ・UdonSharp完全対応
/// ・外部AIなし
/// ・テンプレ＋条件分岐で「推論っぽさ」を生成
/// </summary>
public class ChemExplainGenerator : UdonSharpBehaviour
{
    [Header("Verbosity")]
    [Range(0, 2)]
    public int verbosity = 1; // 0=短い / 1=標準 / 2=少し詳しい

    // =====================================================
    // 外部から呼ばれる生成API
    // =====================================================
    public void Build(
        string inputFormula,
        string toolId,
        string reactionTag,
        float stir01,
        float pour01,
        float heat01,
        float shake01,
        float tempC,
        float reactionPotential,
        float gasLikelihood,
        float energyRelease,
        bool isDangerous,
        out string hint,
        out string explain,
        out string safety
    )
    {
        // -----------------------
        // Hint（次の行動）
        // -----------------------
        if (reactionPotential < 0.25f)
        {
            hint = Pick(inputFormula, toolId, "low", new string[] {
                "変化が弱いです。攪拌や加熱を増やしてみましょう。",
                "反応が進みにくい条件です。操作量を上げて比較してください。",
                "条件不足。温度や攪拌を強めると違いが出ます。"
            });
        }
        else if (reactionPotential > 1.1f)
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
        string obs = BuildObservation(reactionTag, gasLikelihood, energyRelease);

        if (verbosity == 0)
        {
            explain =
                "【推論】" + ModeToJP(reactionTag) + "\n" +
                "【観察】" + obs;
        }
        else
        {
            explain =
                "【推論】器具:" + Safe(toolId) +
                " / モード:" + ModeToJP(reactionTag) + "\n" +
                "【条件】T=" + Mathf.RoundToInt(tempC) + "℃ " +
                "攪拌=" + Pct(stir01) +
                " 加熱=" + Pct(heat01) +
                " 注ぐ=" + Pct(pour01) +
                " 振る=" + Pct(shake01) + "\n" +
                "【観察】" + obs;

            if (verbosity >= 2)
            {
                explain += "\n【学習】操作を1つずつ変えると因果が見えます。";
            }
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
    // 内部ロジック
    // =====================================================

    private string BuildObservation(
        string mode,
        float gas,
        float energy
    )
    {
        string s;

        if (mode == "oxidation")
        {
            s = "発熱・発光・火花が出やすい。";
            if (gas > 0.3f) s += " 煙や気体が観察されます。";
        }
        else if (mode == "chloride")
        {
            s = "混合反応。泡や白煙、色変化が起こります。";
        }
        else if (mode == "dissolve")
        {
            s = "溶解・混合。透明度や波の変化に注目。";
        }
        else
        {
            s = "目立った変化は少ない状態です。";
        }

        if (energy > 0.8f)
        {
            s += " 温度上昇に注意。";
        }

        return s;
    }

    private string ModeToJP(string mode)
    {
        if (mode == "oxidation") return "酸化方向";
        if (mode == "chloride") return "混合反応";
        if (mode == "dissolve") return "溶解";
        return "変化小";
    }

    private string Pct(float v)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";
    }

    // =====================================================
    // UdonSharp対応・安全ハッシュ
    // =====================================================
    private string Pick(string a, string b, string key, string[] arr)
    {
        if (arr == null || arr.Length == 0) return "";

        int h = SimpleHash(a + "|" + b + "|" + key);
        int idx = Mathf.Abs(h) % arr.Length;
        return arr[idx];
    }

    private int SimpleHash(string s)
    {
        int h = 0;
        for (int i = 0; i < s.Length; i++)
        {
            h = (h * 31) + (int)s[i];
        }
        return h;
    }

    private string Safe(string s)
    {
        return s == null ? "" : s;
    }


    // =====================================================
    // Simple wrapper for spawner
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
        // いまは詳細入力（stir/pour/heat/shake etc）は未使用なので0で埋める
        Build(
            inputFormula,
            toolId,
            "auto",
            0f, 0f, 0f, 0f,
            tempC,
            reactionPotential,
            0f,
            0f,
            isDangerous,
            out hint,
            out explain,
            out safety
        );
    }

}
