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
    [Range(0.02f, 0.2f)] public float realtimeTick = 0.1f;
    [Range(0.2f, 1.0f)] public float thinkingTick = 0.4f;

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

    // ★ 内部正式名
    [System.NonSerialized] public string productFormula;
    [System.NonSerialized] public string reactionTag;

    // ★ 互換API（Spawner用）
    public string predictedProductFormula
    {
        get { return productFormula; }
    }

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

    private float _reactionPotential;
    private float _gasLikelihood;
    private float _energyRelease;
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
            reactionPredictor.Predict(
                inputFormula, toolId,
                out productFormula, out reactionTag, out explainText
            );
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
     * Compatibility API (Spawner expects this)
     * ================================ */
    public bool ShouldTick(float deltaTime)
    {
        // 旧Spawnerは「AI側でTick管理していない」前提
        // 新AIは内部でTick管理しているため、常に true でOK
        return true;
    }

    /* ================================
     * Called every frame from Spawner
     * ================================ */
    public void TickRealtime(
        float stir, float pour, float heat, float shake, float tempC)
    {
        if (!isRunning) return;

        float dt = Time.deltaTime;
        _rtAccum += dt;
        _thinkAccum += dt;

        if (_thinkAccum >= thinkingTick)
        {
            _thinkAccum = 0f;
            RunDeepAnalysis(stir, pour, heat, shake, tempC);
        }

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
        ElementState st = ElementState.Solid;
        if (elementDb != null)
            st = elementDb.GetStateFromFormulaAtTemp(inputFormula, tempC);

        _gasLikelihood =
            st == ElementState.Gas ? 1f :
            st == ElementState.Liquid ? 0.4f : 0f;

        float thermal = Mathf.Clamp01((tempC - 20f) / 80f) * temperatureWeight;
        float mixing = Mathf.Clamp01(stir * 0.6f + pour * 0.4f) * mixingWeight;

        _reactionPotential = thermal + mixing;

        _energyRelease =
            reactionTag == "oxidation"
                ? thermal * energyReleaseWeight
                : mixing * 0.5f;

        if (elementDb != null)
        {
            int hz = elementDb.GetHazardFromFormula(inputFormula);
            _isDangerous = hz != 0;
            if (_isDangerous)
                safetyText = elementDb.BuildSafetyHintFromFormula(inputFormula, reactionTag);
        }

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
        float drive =
            heat * 0.4f +
            stir * 0.25f +
            pour * 0.2f +
            shake * 0.15f;

        progress01 = Mathf.Clamp01(progress01 + drive * _reactionPotential * realtimeTick);

        fxHeat = Mathf.Clamp01(_energyRelease);
        fxGlow = Mathf.Clamp01(_energyRelease * 0.8f);
        fxSmoke = Mathf.Clamp01(_gasLikelihood);
        fxWave = Mathf.Clamp01(stir + pour);
        fxFoam = Mathf.Clamp01(stir * 0.6f + pour * 0.4f);
        fxSpark = Mathf.Clamp01(_energyRelease * heat);

        if (progress01 >= 1f)
        {
            isComplete = true;
            isRunning = false;
            explainText = "条件と操作に基づき、反応が完了しました。";
        }
    }
}
