using UdonSharp;
using UnityEngine;

/// <summary>
/// AIRequestSender
/// --------------------------------------------------
/// 外部APIなしの「生成っぽい」係数生成器。
/// ・StartSessionで入力/器具を受け取る
/// ・seed固定で再現性を担保
/// ・TickRealtime or EvaluateAtProgress で可視化用係数を更新
/// --------------------------------------------------
/// </summary>
public class AIRequestSender : UdonSharpBehaviour
{
    [Header("Session State")]
    public bool isRunning;
    public bool isComplete;

    [Header("Deterministic Seed")]
    public bool useOverrideSeed;
    public int sessionSeedOverride;

    [Header("Predicted (for UI / visuals)")]
    public string predictedProductFormula;
    public string predictedReactionTag;

    [Header("Outputs (0..1)")]
    [Range(0f,1f)] public float fxHeat;
    [Range(0f,1f)] public float fxFoam;
    [Range(0f,1f)] public float fxGlow;
    [Range(0f,1f)] public float fxWave;
    [Range(0f,1f)] public float fxSpark;
    [Range(0f,1f)] public float fxSmoke;

    [Header("Progress")]
    [Range(0f,1f)] public float sessionProgress01;

    // internal variation
    private float _v1, _v2, _v3;

    public void StartSession(string formula, string toolId)
    {
        string f = formula == null ? "" : formula.Trim();
        string t = toolId == null ? "" : toolId.Trim();

        int seed = useOverrideSeed ? sessionSeedOverride : ComputeSeed(f, t);
        SeedVariations(seed);

        predictedProductFormula = f;
        predictedReactionTag = PredictTagFromTool(t);

        isRunning = true;
        isComplete = false;
        sessionProgress01 = 0f;
        EvaluateAtProgress(0f);
    }

    public void TickRealtime(float deltaTime, float stir01, float pour01, float heat01, float shake01)
    {
        if (!isRunning) return;

        // 基本進行（操作入力があれば速くなるよう拡張可能）
        float speed = 0.20f + 0.10f * _v1;
        sessionProgress01 = Mathf.Clamp01(sessionProgress01 + deltaTime * speed);

        EvaluateAtProgress(sessionProgress01);

        if (sessionProgress01 >= 0.999f)
        {
            isComplete = true;
        }
    }

    /// <summary>
    /// spectator/同期追従用：progressだけで係数を決定（非同期60fpsで補間）
    /// </summary>
    public void EvaluateAtProgress(float progress01)
    {
        if (!isRunning) return;

        float p = Mathf.Clamp01(progress01);
        sessionProgress01 = p;

        // タグ別にカーブを変える
        if (predictedReactionTag == "oxidation")
        {
            fxHeat = EaseInOut(p);
            fxGlow = EaseOut(p) * (0.7f + 0.3f * _v2);
            fxSmoke = EaseIn(p) * (0.5f + 0.5f * _v3);
            fxSpark = (p > 0.7f) ? (p - 0.7f) / 0.3f * (0.2f + 0.8f * _v1) : 0f;
            fxFoam = 0.05f * (0.5f + 0.5f * _v2);
            fxWave = 0.15f * (0.5f + 0.5f * _v3);
        }
        else if (predictedReactionTag == "chloride" || predictedReactionTag == "mixing")
        {
            fxFoam = EaseInOut(p) * (0.6f + 0.4f * _v1);
            fxWave = EaseInOut(p) * (0.5f + 0.5f * _v2);
            fxGlow = 0.10f * (0.5f + 0.5f * _v3);
            fxHeat = 0.10f * (0.5f + 0.5f * _v2);
            fxSmoke = 0.15f * (0.5f + 0.5f * _v1);
            fxSpark = 0f;
        }

else if (predictedReactionTag == "photo_explosion")
{
    // Strong, highly visible preset for Hydrogen + Chlorine (light) demo
    // Make it obvious even in bright worlds.
    fxSpark = EaseInOut(p) * (0.85f + 0.15f * _v1);
    fxSmoke = EaseInOut(p) * (0.65f + 0.35f * _v2);
    fxGlow  = EaseInOut(p) * (0.70f + 0.30f * _v3);
    fxHeat  = EaseInOut(p) * (0.80f + 0.20f * _v2);
    fxFoam  = 0.05f * (0.5f + 0.5f * _v1);
    fxWave  = 0.10f * (0.5f + 0.5f * _v3);
}

        else
        {
            fxHeat = 0.05f;
            fxFoam = 0.02f;
            fxGlow = 0.02f;
            fxWave = 0.02f;
            fxSpark = 0f;
            fxSmoke = 0.01f;
        }
    }

    public void ResetSession()
    {
        isRunning = false;
        isComplete = false;
        predictedProductFormula = "";
        predictedReactionTag = "none";
        sessionProgress01 = 0f;

        fxHeat = fxFoam = fxGlow = fxWave = fxSpark = fxSmoke = 0f;
    }

    private string PredictTagFromTool(string toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return "none";
        if (toolId.Contains("Gasburner")) return "oxidation";
        if (toolId.Contains("FLASK") || toolId.Contains("Beaker")) return "mixing";
        return "none";
    }

    private void SeedVariations(int seed)
    {
        // LCG
        int s = seed;
        s = (s * 1103515245 + 12345);
        _v1 = ((s >> 16) & 0x7FFF) / 32767f;
        s = (s * 1103515245 + 12345);
        _v2 = ((s >> 16) & 0x7FFF) / 32767f;
        s = (s * 1103515245 + 12345);
        _v3 = ((s >> 16) & 0x7FFF) / 32767f;
    }

    private int ComputeSeed(string a, string b)
    {
        int hash = 17;
        hash = HashStep(hash, a);
        hash = HashStep(hash, b);
        if (hash < 0) hash = -hash;
        return hash;
    }

    private int HashStep(int hash, string s)
    {
        if (s == null) return hash;
        int len = s.Length;
        for (int i = 0; i < len; i++)
        {
            hash = (hash * 31) + (int)s[i];
        }
        return hash;
    }

    private float EaseIn(float t) { return t * t; }
    private float EaseOut(float t) { return 1f - (1f - t) * (1f - t); }
    private float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
