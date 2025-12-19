using UdonSharp;
using UnityEngine;

/// <summary>
/// AIRequestSender
/// --------------------------------------------------
/// ・生成AIは1セッション1回のみ実行
/// ・Resetされるまで結果は固定
/// ・インスタンスが存在する限りデータ保持
/// ・生成物Prefabに「個体差パラメータ」を供給
/// ・UdonSharp完全対応
/// --------------------------------------------------
///
/// 外部想定API：
///   StartSession(string formula, string toolId)
///   ShouldTick(float deltaTime)
///   TickRealtime(float deltaTime, float stir, float pour, float heat, float shake)
///   ResetSession()
/// </summary>
public class AIRequestSender : UdonSharpBehaviour
{
    // =====================================================
    // セッション状態（外部参照用）
    // =====================================================
    [Header("Session State")]
    public bool isRunning;
    public bool isComplete;
    [Range(0f, 1f)] public float progress01;

    // =====================================================
    // 生成結果（固定）
    // =====================================================
    [Header("Generated (Fixed until Reset)")]
    public string reactionTag;
    public string predictedProductFormula;

    [TextArea] public string hintText;
    [TextArea] public string explainText;
    [TextArea] public string safetyText;

    // =====================================================
    // FX（再生用）
    // =====================================================
    [Header("FX Output")]
    [Range(0f, 1f)] public float fxGlow;
    [Range(0f, 1f)] public float fxHeat;
    [Range(0f, 1f)] public float fxFoam;
    [Range(0f, 1f)] public float fxSmoke;
    [Range(0f, 1f)] public float fxSpark;
    [Range(0f, 1f)] public float fxWave;

    // =====================================================
    // 生成物：個体差パラメータ（固定）
    // =====================================================
    [Header("Individual Parameters (for Product Prefab)")]
    [Range(0.5f, 1.5f)] public float indSize;
    [Range(-0.2f, 0.2f)] public float indColorShift;
    [Range(0f, 1f)] public float indRoughness;
    [Range(0f, 1f)] public float indGlow;
    [Range(0f, 1f)] public float indStability;

    // =====================================================
    // 内部キャッシュ（生成AIの記憶）
    // =====================================================
    private bool _hasGenerated;

    private float _cachedReactionStrength;
    private float _cachedGasLevel;
    private float _cachedEnergy;

    private int _sessionSeed;

    private string _inputFormula;
    private string _toolId;

    private float _elapsed;
    private const float SESSION_DURATION = 3.0f;

    // =====================================================
    // ===== 外部から呼ばれるAPI =====
    // =====================================================

    /// <summary>
    /// 実験開始（生成AIはここで1回だけ動く）
    /// </summary>
    public void StartSession(string formula, string toolId)
    {
        _inputFormula = formula;
        _toolId = toolId;

        isRunning = true;
        isComplete = false;
        progress01 = 0f;
        _elapsed = 0f;

        _hasGenerated = false;

        // 初期化
        fxGlow = fxHeat = fxFoam = fxSmoke = fxSpark = fxWave = 0f;
        hintText = explainText = safetyText = "";

        GenerateOnce();
    }

    /// <summary>
    /// Spawner 側の進行判定用
    /// </summary>
    public bool ShouldTick(float deltaTime)
    {
        return isRunning && !isComplete;
    }

    /// <summary>
    /// 実験進行（再生フェーズ）
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

        // ===== 再生（生成しない）=====
        fxHeat = _cachedEnergy * progress01;
        fxGlow = _cachedEnergy * 0.8f * progress01;
        fxFoam = _cachedGasLevel * _cachedReactionStrength * progress01;
        fxSmoke = _cachedGasLevel * heat * progress01;
        fxSpark = reactionTag == "oxidation" ? heat * progress01 : 0f;
        fxWave = (stir + pour) * progress01;

        if (progress01 >= 1f)
        {
            isRunning = false;
            isComplete = true;
        }
    }

    /// <summary>
    /// リセット（生成AIの記憶を破棄）
    /// </summary>
    public void ResetSession()
    {
        isRunning = false;
        isComplete = false;
        progress01 = 0f;

        _hasGenerated = false;

        reactionTag = "";
        predictedProductFormula = "";

        hintText = explainText = safetyText = "";

        fxGlow = fxHeat = fxFoam = fxSmoke = fxSpark = fxWave = 0f;
    }

    // =====================================================
    // ===== 生成AIコア（1回だけ実行）=====
    // =====================================================

    private void GenerateOnce()
    {
        if (_hasGenerated) return;
        _hasGenerated = true;

        // -------------------------------------------------
        // セッション固有Seed（完全新規・固定）
        // -------------------------------------------------
        _sessionSeed = (int)(Time.time * 1000f) % 1000000;
        Random.InitState(_sessionSeed);

        // -------------------------------------------------
        // 疑似推論（ルール＋生成）
        // -------------------------------------------------
        reactionTag = InferReactionTag(_inputFormula, _toolId);
        predictedProductFormula = InferProduct(_inputFormula, reactionTag);

        _cachedReactionStrength = Random.Range(0.4f, 0.9f);
        _cachedGasLevel = Random.Range(0.1f, 0.7f);
        _cachedEnergy = Random.Range(0.2f, 0.95f);

        // -------------------------------------------------
        // 個体差パラメータ生成（生成物用）
        // -------------------------------------------------
        indSize = Random.Range(0.8f, 1.3f);
        indColorShift = Random.Range(-0.15f, 0.15f);
        indRoughness = Random.Range(0.2f, 0.8f);
        indGlow = Random.Range(0.0f, 0.9f);
        indStability = Random.Range(0.3f, 1.0f);

        // -------------------------------------------------
        // テキスト生成（完全新規・固定）
        // -------------------------------------------------
        hintText = Pick(new string[] {
            "条件を少し変えて反応の違いを観察してください。",
            "操作を1つずつ変えると因果が見えます。",
            "攪拌や加熱を調整して変化を比較しましょう。"
        });

        explainText =
            "【生成推論】この実験は「" + reactionTag + "」方向の変化を示します。\n" +
            "【個体差】サイズ=" + Percent(indSize) +
            " / 色ずれ=" + Percent(Mathf.Abs(indColorShift)) +
            " / 粗さ=" + Percent(indRoughness) + "\n" +
            "【注記】この結果は本セッション固有です。";

        safetyText =
            _cachedEnergy > 0.75f
                ? "注意：反応エネルギーが高めです。演出に従って観察してください。"
                : "安全：通常の観察条件です。";
    }

    // =====================================================
    // ===== 内部ユーティリティ =====
    // =====================================================

    private string InferReactionTag(string formula, string tool)
    {
        if (tool == "burner") return "oxidation";
        if (tool == "beaker") return "dissolve";
        return "mix";
    }

    private string InferProduct(string formula, string tag)
    {
        if (tag == "oxidation") return formula + "_OX";
        if (tag == "dissolve") return formula + "_AQ";
        return formula + "_MIX";
    }

    private string Pick(string[] list)
    {
        if (list == null || list.Length == 0) return "";
        return list[Random.Range(0, list.Length)];
    }

    private string Percent(float v)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";
    }
}
