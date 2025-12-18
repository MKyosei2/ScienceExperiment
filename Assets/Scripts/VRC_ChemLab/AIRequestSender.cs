using UdonSharp;
using UnityEngine;

/// <summary>
/// AIRequestSender
/// ・ChemElementSpawner 完全互換
/// ・ReactionPredictor 実API完全準拠
/// ・外部AIなし（UdonSharp内完結）
/// ・VFX / SFX / 教育テキストを統合制御
/// </summary>
public class AIRequestSender : UdonSharpBehaviour
{
    // =====================================================
    // 外部参照
    // =====================================================
    [Header("External")]
    public ReactionPredictor reactionPredictor;
    public ChemExplainGenerator explainGenerator;

    // =====================================================
    // Spawner が参照する状態
    // =====================================================
    [Header("Session State")]
    public bool isRunning;
    public bool isComplete;
    [Range(0f, 1f)] public float progress01;

    public string predictedProductFormula;
    public string reactionTag;

    // =====================================================
    // FX（視覚・音）
    // =====================================================
    [Header("FX Output")]
    [Range(0f, 1f)] public float fxGlow;
    [Range(0f, 1f)] public float fxHeat;
    [Range(0f, 1f)] public float fxFoam;
    [Range(0f, 1f)] public float fxSmoke;
    [Range(0f, 1f)] public float fxSpark;
    [Range(0f, 1f)] public float fxWave;

    // =====================================================
    // UIテキスト
    // =====================================================
    [Header("Text Output")]
    [TextArea] public string hintText;
    [TextArea] public string explainText;
    [TextArea] public string safetyText;

    // =====================================================
    // 内部
    // =====================================================
    private string _inputFormula;
    private string _toolId;

    private float _elapsed;
    private const float SESSION_DURATION = 3.0f;

    // =====================================================
    // ChemElementSpawner 互換 API（※完全一致）
    // =====================================================

    /// <summary>
    /// セッション開始
    /// </summary>
    public void StartSession(string formula, string toolId)
    {
        _inputFormula = formula;
        _toolId = toolId;

        isRunning = true;
        isComplete = false;
        progress01 = 0f;
        _elapsed = 0f;

        // 初期化
        predictedProductFormula = "";
        reactionTag = "none";

        fxGlow = fxHeat = fxFoam = fxSmoke = fxSpark = fxWave = 0f;
        hintText = explainText = safetyText = "";

        // 初回推論（操作量ゼロ）
        RunInference(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Spawner から毎フレーム呼ばれる
    /// </summary>
    public bool ShouldTick(float deltaTime)
    {
        return isRunning && !isComplete;
    }

    /// <summary>
    /// 実験進行
    /// </summary>
    public void TickRealtime(
        float deltaTime,
        float stir,
        float pour,
        float heat,
        float shake
    )
    {
        if (!isRunning || isComplete) return;

        _elapsed += deltaTime;
        progress01 = Mathf.Clamp01(_elapsed / SESSION_DURATION);

        RunInference(stir, pour, heat, shake);

        if (progress01 >= 1f)
        {
            isRunning = false;
            isComplete = true;
        }
    }

    // =====================================================
    // 推論コア（U#内 疑似AI）
    // =====================================================

    private void RunInference(
        float stir,
        float pour,
        float heat,
        float shake
    )
    {
        // -------- ReactionPredictor 正式API --------
        string localSafetyHint = "";

        if (reactionPredictor != null)
        {
            reactionPredictor.Predict(
                _inputFormula,
                _toolId,
                out reactionTag,
                out predictedProductFormula,
                out localSafetyHint
            );
        }

        // -------- スコアリング（AI風） --------
        float reactionPotential =
            stir * 0.4f +
            pour * 0.3f +
            heat * 0.8f +
            shake * 0.2f;

        float gasLikelihood = Mathf.Clamp01(
            (reactionTag == "chloride" ? 0.6f : 0.2f) +
            heat * 0.2f +
            shake * 0.2f
        );

        float energyRelease = Mathf.Clamp01(
            (reactionTag == "oxidation" ? 0.7f : 0.3f) +
            heat * 0.5f
        );

        // -------- FX生成（progress連動） --------
        fxHeat = energyRelease * progress01;
        fxGlow = energyRelease * 0.8f * progress01;
        fxFoam = gasLikelihood * reactionPotential * progress01;
        fxSmoke = gasLikelihood * heat * progress01;
        fxSpark = reactionTag == "oxidation" ? heat * progress01 : 0f;
        fxWave = (stir + pour) * progress01;

        // -------- 教育用テキスト --------
        if (explainGenerator != null)
        {
            explainGenerator.Build(
                _inputFormula,
                _toolId,
                reactionTag,
                stir, pour, heat, shake,
                25f,
                reactionPotential,
                gasLikelihood,
                energyRelease,
                localSafetyHint.Length > 0,
                out hintText,
                out explainText,
                out safetyText
            );
        }
        else
        {
            hintText = "操作量を変えて反応の違いを観察してください。";
            explainText = "反応タイプ：" + reactionTag;
            safetyText = localSafetyHint;
        }
    }
}
