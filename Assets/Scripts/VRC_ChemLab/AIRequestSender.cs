using UdonSharp;
using UnityEngine;

public class AIRequestSender : UdonSharpBehaviour
{
    /* ================================
     * References
     * ================================ */
    public ChemElementDatabase elementDb;
    public ReactionPredictor reactionPredictor;

    /* ================================
     * Timing
     * ================================ */
    [Header("Timing")]
    [Range(0.02f, 0.2f)] public float realtimeTick = 0.1f;   // 見た目・反応（速い）
    [Range(0.2f, 1.0f)] public float thinkingTick = 0.4f;   // 重度分析（遅い）

    /* ================================
     * Science tuning
     * ================================ */
    [Header("Science Sensitivity")]
    [Range(0f, 3f)] public float temperatureWeight = 1.2f;
    [Range(0f, 3f)] public float mixingWeight = 1.0f;
    [Range(0f, 3f)] public float energyReleaseWeight = 1.0f;

    /* ================================
     * Runtime state
     * ================================ */
    [System.NonSerialized] public bool isRunning;
    [System.NonSerialized] public bool isComplete;

    [System.NonSerialized] public string inputFormula;
    [System.NonSerialized] public string toolId;
    [System.NonSerialized] public string productFormula;
    [System.NonSerialized] public string reactionTag;

    [System.NonSerialized] public float progress01;

    /* ================================
     * FX outputs (0..1)
     * ================================ */
    [System.NonSerialized] public float fxHeat;
    [System.NonSerialized] public float fxFoam;
    [System.NonSerialized] public float fxGlow;
    [System.NonSerialized] public float fxSmoke;
    [System.NonSerialized] public float fxWave;
    [System.NonSerialized] public float fxSpark;

    /* ================================
     * Text outputs
     * ================================ */
    [System.NonSerialized] public string hintText;
    [System.NonSerialized] public string explainText;
    [System.NonSerialized] public string safetyText;

    /* ================================
     * Internal accumulators
     * ================================ */
    private float _rtAccum;
    private float _thinkAccum;

    // 内部“思考結果”（高速レイヤーが参照）
    private float _reactionPotential;   // この条件で反応が起きやすいか
    private float _gasLikelihood;        // 気体・煙方向
    private float _energyRelease;        // 発熱・発光方向
    private bool _isDangerous;

    /* ================================
     * Session start
     * ================================ */
    public void StartSession(string formula, string equipment)
    {
        inputFormula = formula;
        toolId = equipment;

        isRunning = true;
        isComplete = false;
        progress01 = 0f;

        fxHeat = fxFoam = fxGlow = fxSmoke = fxWave = fxSpark = 0f;
        hintText = "";
        explainText = "";
        safetyText = "";

        if (reactionPredictor != null)
        {
            reactionPredictor.Predict(inputFormula, toolId,
                out productFormula, out reactionTag, out explainText);
        }
        else
        {
            productFormula = inputFormula;
            reactionTag = "none";
        }

        _reactionPotential = 0f;
        _gasLikelihood = 0f;
        _energyRelease = 0f;
        _isDangerous = false;

        _rtAccum = 0f;
        _thinkAccum = 0f;
    }

    /* ================================
     * Called from Spawner (every frame)
     * ================================ */
    public void TickRealtime(
        float stir, float pour, float heat, float shake, float tempC)
    {
        if (!isRunning) return;

        float dt = Time.deltaTime;
        _rtAccum += dt;
        _thinkAccum += dt;

        /* ---------- Layer 2 : Deep Thinking ---------- */
        if (_thinkAccum >= thinkingTick)
        {
            _thinkAccum = 0f;
            RunDeepAnalysis(stir, pour, heat, shake, tempC);
        }

        /* ---------- Layer 1 : Instant Reaction ---------- */
        if (_rtAccum >= realtimeTick)
        {
            _rtAccum = 0f;
            RunFastReaction(stir, pour, heat, shake);
        }
    }

    /* ================================
     * Layer 2 : Extended Thinking
     * ================================ */
    private void RunDeepAnalysis(
        float stir, float pour, float heat, float shake, float tempC)
    {
        // 1) 状態評価
        ElementState st = ElementState.Solid;
        if (elementDb != null)
            st = elementDb.GetStateFromFormulaAtTemp(inputFormula, tempC);

        _gasLikelihood =
            (st == ElementState.Gas) ? 1f :
            (st == ElementState.Liquid) ? 0.4f : 0f;

        // 2) 反応ポテンシャル
        float thermal = Mathf.Clamp01((tempC - 20f) / 80f) * temperatureWeight;
        float mixing = Mathf.Clamp01(stir * 0.6f + pour * 0.4f) * mixingWeight;

        _reactionPotential = thermal + mixing;

        // 3) エネルギー解放傾向
        _energyRelease =
            reactionTag == "oxidation"
                ? thermal * energyReleaseWeight
                : mixing * 0.5f;

        // 4) 危険性評価
        if (elementDb != null)
        {
            int hz = elementDb.GetHazardFromFormula(inputFormula);
            _isDangerous = hz != 0;
            if (_isDangerous)
                safetyText = elementDb.BuildSafetyHintFromFormula(inputFormula, reactionTag);
        }

        // 5) メタ判断（ヒント生成）
        if (_reactionPotential < 0.2f)
            hintText = "条件が弱いです。加熱や攪拌を試してください。";
        else if (_reactionPotential > 1.2f)
            hintText = "反応が活発です。変化を観察してください。";
        else
            hintText = "条件を調整して反応の違いを比較しましょう。";
    }

    /* ================================
     * Layer 1 : Fast Reaction
     * ================================ */
    private void RunFastReaction(
        float stir, float pour, float heat, float shake)
    {
        // 進行度（即応）
        float drive =
            heat * 0.4f +
            stir * 0.25f +
            pour * 0.2f +
            shake * 0.15f;

        progress01 = Mathf.Clamp01(progress01 + drive * _reactionPotential * realtimeTick);

        // FXは“思考結果”を即座に反映
        fxHeat = Mathf.Clamp01(_energyRelease);
        fxGlow = Mathf.Clamp01(_energyRelease * 0.8f);
        fxSmoke = Mathf.Clamp01(_gasLikelihood);
        fxWave = Mathf.Clamp01(stir + pour);
        fxFoam = Mathf.Clamp01(mixingClamp(stir, pour));
        fxSpark = Mathf.Clamp01(_energyRelease * heat);

        if (progress01 >= 1f)
        {
            isComplete = true;
            isRunning = false;
            explainText = "条件と操作に基づき、反応が完了しました。";
        }
    }

    private float mixingClamp(float s, float p)
    {
        return Mathf.Clamp01(s * 0.6f + p * 0.4f);
    }
}
