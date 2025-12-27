using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;
using System.Text;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("Core")]
    public ChemElementDatabase elementDb;
    public ChemEnvironmentManager environment; // 既存がある前提（無い場合はnull可）
    public AIRequestSender ai;

    [Header("Logic / Explain (optional)")]
    public ReactionPredictor predictor;
    public ChemExplainGenerator explainGenerator;
    public ChemicalReactionDatabase compoundDb;

    [Header("Determinism (recommended for school)")]
    public bool deterministicSeed = true;
    public int fixedSeed = 0; // 0=auto hash

    [Header("Visual")]
    public ChemVisualController sampleVisual;     // 元素/生成物の見た目
    public ChemReactionAnimator reactionAnimator; // 泡/熱/煙/発光など
    public Transform heatSource;                  // バーナー等（無ければnull）

    [Header("Optional UI (no TMP dependency)")]
    public Text hintText;
    public Text explainText;
    public Text safetyText;
    public Text debugText;

    [Header("VR Motion Sensing")]
    public bool realtimeOnlyInVR = true;
    public float stirRadiusMeters = 0.25f;      // 混ぜ判定：容器の近くで手が動く
    public float stirSpeedToMax = 2.0f;         // これ以上の手速度でstir=1
    public float shakeAccelToMax = 12.0f;       // これ以上の加速度でshake=1
    public float pourTiltToMaxDeg = 80f;        // これ以上の傾きでpour=1
    public float pourTiltStartDeg = 25f;        // これ以下はpour=0
    public float heatNearMeters = 0.20f;        // 熱源近い
    public float heatFarMeters = 0.80f;         // 熱源遠い

    [Header("Target (what player manipulates)")]
    public Transform containerTransform; // ビーカー等。無ければこのオブジェクト自身

    // ---- 状態 ----
    private string _selectedInput = "H";   // 元素記号または式
    private string _selectedTool = "None"; // 器具ID
    private string _currentProduct = "";  // 完了したら入る

    // ---- 履歴（ChemStatusDisplay互換用）----
    private StringBuilder _history = new StringBuilder(2048);
    private const int HISTORY_MAX_CHARS = 12000;

    private VRCPlayerApi _lp;
    private Vector3 _prevRHPos;
    private Vector3 _prevLHPos;
    private Vector3 _prevRHVel;
    private Vector3 _prevLHVel;
    private float _dtSafe;

    private void Start()
    {
        _lp = Networking.LocalPlayer;
        if (containerTransform == null) containerTransform = transform;

        AppendHistory("Spawner Start");

        // 初期見た目
        UpdateSampleAppearance(_selectedInput);
        WriteUI();
    }

    // =========================
    // ChemStatusDisplay 互換API
    // =========================
    public string GetLastElement()
    {
        // 旧挙動に寄せる：最後に選択した入力（元素/式）
        return _selectedInput;
    }

    public string GetLastEquipment()
    {
        return _selectedTool;
    }

    public string GetHistoryLog()
    {
        return _history.ToString();
    }

    // ---- 既存フロー：ボタンから呼ばれる想定 ----
    public void SelectElement(string symbolOrFormula)
    {
        _selectedInput = (symbolOrFormula == null) ? "" : symbolOrFormula.Trim();
        _currentProduct = ""; // 入力が変わったら生成物は一旦クリア

        AppendHistory("SelectElement: " + _selectedInput);

        UpdateSampleAppearance(_selectedInput);
        WriteUI();
    }

    public void SelectEquipment(string toolId)
    {
        _selectedTool = (toolId == null) ? "" : toolId.Trim();

        AppendHistory("SelectEquipment: " + _selectedTool);

        WriteUI();
    }

    // 実験開始（既存の「開始ボタン」想定）
    public void _StartExperiment()
    {
        if (ai == null)
        {
            AppendHistory("ERROR: AIRequestSender is null.");
            AppendDebug("AIRequestSender is null.");
            return;
        }
        // AIに環境/DB/説明器を渡す（任意）
        ai.envTempC = GetEnvTempC();
        ai.envHumidity = GetEnvHumidity();
        ai.envPressure = GetEnvPressure();
        ai.elementDb = elementDb;
        ai.predictor = predictor;
        ai.explainGenerator = explainGenerator;
        ai.compoundDb = compoundDb;

        // 学校/科学館向け：入力＋環境が同じなら同じ結果になる Seed
        if (deterministicSeed)
        {
            int seed = fixedSeed != 0
                ? fixedSeed
                : ComputeSeed(_selectedInput, _selectedTool, ai.envTempC, ai.envHumidity, ai.envPressure);
            ai.useOverrideSeed = true;
            ai.sessionSeedOverride = seed;
        }
        else
        {
            ai.useOverrideSeed = false;
        }

        ai.StartSession(_selectedInput, _selectedTool);

        AppendHistory("Experiment Start: input=" + _selectedInput + " tool=" + _selectedTool);
        AppendHistory("Predicted Product: " + ai.predictedProductFormula + " / Tag: " + ai.reactionTag);

        // 進行中は入力見た目、完了で生成物へ切替（Update側で実施）
        WriteUI();
    }

    // 実験リセット（リセットボタン/Orchestratorから呼ぶ）
    public void _ResetExperiment()
    {
        _currentProduct = "";
        _selectedInput = "H";
        _selectedTool = "None";

        // ログクリア
        if (_history != null) _history.Length = 0;
        AppendHistory("Experiment Reset");

        // AI リセット
        if (ai != null) ai.ResetSession();

        // 演出停止
        if (reactionAnimator != null) reactionAnimator.StopAll();

        // 見た目初期化
        if (sampleVisual != null)
        {
            sampleVisual.SetElementAppearance(Color.white, ElementState.Liquid, null);
            sampleVisual.UpdateEnvironment(GetEnvTempC(), GetEnvHumidity(), GetEnvPressure());
        }

        WriteUI();
        WriteUIRealtime();
    }

    private void Update()
    {
        if (ai == null) return;

        // VRのみでリアルタイム、という要件
        if (realtimeOnlyInVR)
        {
            if (_lp == null) _lp = Networking.LocalPlayer;
            if (_lp == null || !_lp.IsUserInVR()) return;
        }

        if (!ai.isRunning) return;

        _dtSafe = Time.deltaTime;
        if (_dtSafe <= 0f) _dtSafe = 0.016f;

        // AI tick 間引き
        if (!ai.ShouldTick(_dtSafe)) return;

        // センサー入力算出
        float stir01, pour01, heat01, shake01;
        ComputeMotionInputs(out stir01, out pour01, out heat01, out shake01);

        float envTemp = GetEnvTempC();
        ai.envTempC = envTemp;
        ai.envHumidity = GetEnvHumidity();
        ai.envPressure = GetEnvPressure();

        // AIへ入力
        ai.TickRealtime(_dtSafe, stir01, pour01, heat01, shake01);

        // 演出反映（連続制御）
        if (reactionAnimator != null)
        {
            reactionAnimator.SetHeatLevel(ai.fxHeat);
            reactionAnimator.SetFoamLevel(ai.fxFoam);
            reactionAnimator.SetGlowLevel(ai.fxGlow);
            reactionAnimator.SetWaveLevel(ai.fxWave);
            reactionAnimator.SetSparkLevel(ai.fxSpark);
            reactionAnimator.SetSmokeLevel(ai.fxSmoke);
        }

        // 完了したら生成物へ見た目を切り替える
        if (ai.isComplete)
        {
            if (!string.IsNullOrEmpty(ai.predictedProductFormula))
            {
                if (_currentProduct != ai.predictedProductFormula)
                {
                    _currentProduct = ai.predictedProductFormula;
                    AppendHistory("Experiment Complete: product=" + _currentProduct);
                }
                UpdateSampleAppearance(_currentProduct);
            }
        }
        else
        {
            // 進行中は入力の見た目
            UpdateSampleAppearance(_selectedInput);
        }

        WriteUIRealtime();
    }

    private void ComputeMotionInputs(out float stir01, out float pour01, out float heat01, out float shake01)
    {
        stir01 = 0f; pour01 = 0f; heat01 = 0f; shake01 = 0f;

        if (_lp == null) _lp = Networking.LocalPlayer;
        if (_lp == null) return;

        // 手トラッキング
        VRCPlayerApi.TrackingData rh = _lp.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
        VRCPlayerApi.TrackingData lh = _lp.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);

        Vector3 rhPos = rh.position;
        Vector3 lhPos = lh.position;

        Vector3 rhVel = (rhPos - _prevRHPos) / Mathf.Max(_dtSafe, 0.01f);
        Vector3 lhVel = (lhPos - _prevLHPos) / Mathf.Max(_dtSafe, 0.01f);

        Vector3 rhAcc = (rhVel - _prevRHVel) / Mathf.Max(_dtSafe, 0.01f);
        Vector3 lhAcc = (lhVel - _prevLHVel) / Mathf.Max(_dtSafe, 0.01f);

        _prevRHPos = rhPos; _prevLHPos = lhPos;
        _prevRHVel = rhVel; _prevLHVel = lhVel;

        Vector3 center = containerTransform != null ? containerTransform.position : transform.position;

        // 1) stir：容器近くで手が速く動くほど
        float rhNear = 1f - Mathf.Clamp01(Vector3.Distance(rhPos, center) / Mathf.Max(stirRadiusMeters, 0.01f));
        float lhNear = 1f - Mathf.Clamp01(Vector3.Distance(lhPos, center) / Mathf.Max(stirRadiusMeters, 0.01f));

        float rhSpeed01 = Mathf.Clamp01(rhVel.magnitude / Mathf.Max(stirSpeedToMax, 0.01f)) * rhNear;
        float lhSpeed01 = Mathf.Clamp01(lhVel.magnitude / Mathf.Max(stirSpeedToMax, 0.01f)) * lhNear;

        stir01 = Mathf.Clamp01(Mathf.Max(rhSpeed01, lhSpeed01));

        // 2) shake：加速度が大きいほど
        float rhShake01 = Mathf.Clamp01(rhAcc.magnitude / Mathf.Max(shakeAccelToMax, 0.01f));
        float lhShake01 = Mathf.Clamp01(lhAcc.magnitude / Mathf.Max(shakeAccelToMax, 0.01f));
        shake01 = Mathf.Clamp01(Mathf.Max(rhShake01, lhShake01));

        // 3) pour：容器の傾き
        if (containerTransform == null) containerTransform = transform;

        float tiltDeg = Vector3.Angle(containerTransform.up, Vector3.up); // 0=直立、90=横倒し
        if (tiltDeg <= pourTiltStartDeg) pour01 = 0f;
        else if (tiltDeg >= pourTiltToMaxDeg) pour01 = 1f;
        else pour01 = Mathf.InverseLerp(pourTiltStartDeg, pourTiltToMaxDeg, tiltDeg);

        // 4) heat：容器と熱源の距離
        if (heatSource != null && containerTransform != null)
        {
            float d = Vector3.Distance(containerTransform.position, heatSource.position);
            if (d <= heatNearMeters) heat01 = 1f;
            else if (d >= heatFarMeters) heat01 = 0f;
            else heat01 = 1f - Mathf.InverseLerp(heatNearMeters, heatFarMeters, d);
        }
        else
        {
            heat01 = 0f;
        }
    }

    private void UpdateSampleAppearance(string symbolOrFormula)
    {
        if (elementDb == null || sampleVisual == null) return;

        float temp = GetEnvTempC();
        Color c = elementDb.GetColorFromFormula(symbolOrFormula);
        ElementState st = elementDb.GetStateFromFormulaAtTemp(symbolOrFormula, temp);

        Material mat = elementDb.GetMaterialFromFormula(symbolOrFormula, st);

        sampleVisual.SetElementAppearance(c, st, mat);
        sampleVisual.UpdateEnvironment(temp, GetEnvHumidity(), GetEnvPressure());
    }

    private float GetEnvTempC()
    {
        if (environment != null) return environment.Temperature;
        return 25f;
    }
    private float GetEnvHumidity()
    {
        if (environment != null) return environment.Humidity;
        return 50f;
    }
    private float GetEnvPressure()
    {
        if (environment != null) return environment.Pressure;
        return 101f;
    }

    // 入力・条件から再現可能なSeedを作る（学校/科学館向け）
    private int ComputeSeed(string input, string tool, float tempC, float humidity, float pressure)
    {
        // UdonSharp は long の剰余演算などが不安定になりやすいので、int 範囲で安全に回す
        // mod を小さく保つことで (hash * 31) が int を超えないようにする
        const int mod = 1000000; // 0〜999,999

        int hash = 17;

        hash = (hash * 31 + HashString(input, mod)) % mod;
        hash = (hash * 31 + HashString(tool, mod)) % mod;

        int t = Mathf.RoundToInt(tempC * 10f);
        int h = Mathf.RoundToInt(humidity * 10f);
        int p = Mathf.RoundToInt(pressure * 10f);

        // 値の取り込み（mod が小さいのでオーバーフローしない）
        hash = (hash * 31 + (t & 0x7fffffff)) % mod;
        hash = (hash * 31 + (h & 0x7fffffff)) % mod;
        hash = (hash * 31 + (p & 0x7fffffff)) % mod;

        // seed 0 は避ける
        if (hash == 0) hash = 1;
        return hash;
    }

    private int HashString(string s, int mod)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int h = 0;
        for (int i = 0; i < s.Length; i++)
        {
            h = (h * 31 + (int)s[i]) % mod;
        }
        return h;
    }



    private void WriteUI()
    {
        if (hintText != null) hintText.text = "";
        if (explainText != null) explainText.text = "";
        if (safetyText != null) safetyText.text = "";

        if (debugText != null)
        {
            debugText.text =
                "Input: " + _selectedInput + "\n" +
                "Tool: " + _selectedTool + "\n" +
                "Product: " + (_currentProduct == "" ? "-" : _currentProduct);
        }
    }

    private void WriteUIRealtime()
    {
        if (ai == null) return;

        if (hintText != null) hintText.text = ai.hintText;
        if (safetyText != null) safetyText.text = ai.safetyText;

        // 完了したら解説を出す
        if (ai.isComplete && explainText != null)
        {
            explainText.text = ai.explainText;
        }

        if (debugText != null)
        {
            debugText.text =
                "Input: " + _selectedInput + "\n" +
                "Tool: " + _selectedTool + "\n" +
                "Progress: " + Mathf.RoundToInt(ai.progress01 * 100f) + "%\n" +
                "Product: " + (ai.isComplete ? ai.predictedProductFormula : "-");
        }
    }

    private void AppendDebug(string msg)
    {
        if (debugText == null) return;
        debugText.text = (debugText.text + "\n" + msg);
    }

    private void AppendHistory(string msg)
    {
        if (_history == null) _history = new StringBuilder(2048);

        // 簡易タイムスタンプ（秒）
        _history.Append("[t=");
        _history.Append(Time.time.ToString("0.0"));
        _history.Append("] ");
        _history.AppendLine(msg);

        // 肥大化防止（末尾を残す）
        if (_history.Length > HISTORY_MAX_CHARS)
        {
            string tail = _history.ToString(_history.Length - (HISTORY_MAX_CHARS / 2), (HISTORY_MAX_CHARS / 2));
            _history.Length = 0;
            _history.AppendLine("...(trimmed)...");
            _history.Append(tail);
        }
    }
}
