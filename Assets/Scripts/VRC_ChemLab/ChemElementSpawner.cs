using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

/// <summary>
/// ChemElementSpawner (Network-authoritative experiment core)
/// ---------------------------------------------------------
/// 同期：実験の真実（操作者/選択/環境/反応結果/進行度/温度）
/// 非同期：見た目（演出/補間/粒子/状態変化の表示）
///
/// 注意：VRChat/Udon同期で60fps配信は現実的ではないため、
///       ネット同期はイベント/低頻度更新、表示は各クライアントで60fps更新する設計です。
/// </summary>
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("Databases / Logic")]
    public ChemElementDatabase elementDb;
    public ChemicalReactionDatabase reactionDb;     // 任意（説明用）
    public ReactionPredictor predictor;             // 任意（結果ID/タグ推定）
    public ChemExplainGenerator explainGenerator;   // 任意（外部AIなし説明）

    [Header("Environment (optional)")]
    public ChemEnvironmentManager environment;      // UI操作の受け口（確定時のみ同期）

    [Header("Local Visual (async)")]
    public bool showProductWhenComplete = true;   // 完了後は生成物を表示
    public ChemVisualController sampleVisual;       // 見た目（固液気/色）
    public ChemReactionAnimator reactionAnimator;   // 泡/煙/熱/発光など（ローカル演出）
    public AIRequestSender ai;                      // VFX係数生成（ローカル）

    [Header("UI (optional, no TMP dependency)")]
    public Text hintText;
    public Text explainText;
    public Text safetyText;
    public Text debugText;

    [Header("Networking")]
    [Tooltip("操作者が進行度を同期する最小間隔（秒）。0.1=10Hz程度。")]
    public float progressSyncInterval = 0.12f;

    [Tooltip("操作者が温度を同期する最小間隔（秒）。表示は各クライアントで60fps補間。")]
    public float temperatureSyncInterval = 0.25f;

    [Header("Dynamic Temperature Model")]
    [Tooltip("実験中に温度を動かす（同期は低頻度、見た目は各クライアントで60fps更新）")]
    public bool enableDynamicTemperature = true;

    [Tooltip("外部加熱(ヒーター)の最大上昇量（℃）。Heat01=1で ambient+この値 を目標にします")]
    public float externalHeatMaxDeltaC = 250f;

    [Tooltip("反応由来の熱(0..1)を温度に足す最大上昇量（℃）。ai.fxHeat を使用")]
    public float reactionHeatMaxDeltaC = 80f;

    [Tooltip("温度が目標へ近づく速度（℃/秒）。")]
    public float thermalResponseCPerSec = 35f;

    [Tooltip("見た目温度（非同期）が同期温度へ追従する速さ（大きいほどキビキビ）")]
    public float visualTempLerpSpeed = 10f;

    [Header("Heater Input (optional)")]
    [Tooltip("true の場合、containerTransform と heatSource の距離から Heat01 を自動算出します（操作者のみ）。")]
    public bool autoHeatFromProximity = false;

    public Transform containerTransform;
    public Transform heatSource;
    public float heatNearMeters = 0.20f;
    public float heatFarMeters = 0.80f;

    // -----------------------------
    // Synced experiment "truth"
    // -----------------------------
    [UdonSynced] private int _syncedVersion;              // 主要変更カウンタ（選択/開始/リセット/操作者変更）
    [UdonSynced] private int _syncedOperatorPlayerId = -1;

    [UdonSynced] private string _syncedInput;            // 元素/式（ボタンのidOrName）
    [UdonSynced] private string _syncedTool;             // 器具ID

    // 環境（同期）
    [UdonSynced] private float _syncedTempC;             // 現在温度（実験中は動的に更新）
    [UdonSynced] private float _syncedAmbientTempC;      // 基準温度（初期条件/環境設定）
    [UdonSynced] private float _syncedPressureKPa;
    [UdonSynced] private float _syncedHumidity;

    // 温度操作（同期）
    [UdonSynced] private float _syncedHeat01;            // 0..1 外部加熱レベル（ボタン or 自動近接）

    [UdonSynced] private string _syncedReactionTag;      // "oxidation"/"chloride"/"none" etc.
    [UdonSynced] private string _syncedProductFormula;   // 生成物（式/ラベル）
    [UdonSynced] private int _syncedSeed;                // 再現性確保用

    [UdonSynced] private int _syncedPhase;               // 0=idle 1=running 2=complete
    [UdonSynced] private float _syncedProgress01;        // 0..1（演出を揃えたい場合）

    // -----------------------------
    // Local cache
    // -----------------------------
    private int _lastAppliedVersion = -1;
    private int _lastAppliedPhase = -1;
    private float _nextProgressSyncAt;
    private float _nextTempSyncAt;
    private string _history = "";
    private string _localInput = "";
    private string _localTool = "";

    // Local temperature (operator simulation + everyone visual smoothing)
    private float _simTempC;
    private float _visualTempC;
    private bool _tempInitialized;

    private void Start()
    {
        if (containerTransform == null) containerTransform = transform;

        // 初期環境を同期変数へ取り込み（オフライン/単独テスト向け）
        PullEnvironmentForSync(true);

        _lastAppliedPhase = _syncedPhase;

        // 温度初期化
        _simTempC = _syncedTempC;
        _visualTempC = _syncedTempC;
        _tempInitialized = true;

        if (sampleVisual != null) sampleVisual.NotifyExperimentReset();

        ApplyVisualFromState(true);
        WriteUI();
    }

    // =====================================================
    // Role management (operator / spectator)
    // =====================================================
    public bool IsOperatorLocal()
    {
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp == null) return true; // offline
        return lp.playerId == _syncedOperatorPlayerId;
    }

    public bool HasOperator()
    {
        return _syncedOperatorPlayerId >= 0;
    }

    private bool EnsureCanControl()
    {
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp == null) return true;

        // operator未選択なら、押した人を操作者にする（自動取得）
        if (_syncedOperatorPlayerId < 0)
        {
            ClaimOperator(lp);
            return true;
        }

        if (lp.playerId == _syncedOperatorPlayerId) return true;

        // spectator：操作拒否
        AppendDebug("Spectator: 操作者のみ操作できます。");
        return false;
    }

    private void ClaimOperator(VRCPlayerApi lp)
    {
        // オーナーを取る（このオブジェクトの同期を送るため）
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(lp, gameObject);
        }

        _syncedOperatorPlayerId = lp.playerId;
        _syncedVersion++;
        RequestSerialization();

        AppendHistory("ClaimOperator: " + lp.playerId);
        WriteUI();
    }

    public void _ReleaseOperator()
    {
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp == null) return;
        if (lp.playerId != _syncedOperatorPlayerId) return;

        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(lp, gameObject);
        }

        _syncedOperatorPlayerId = -1;
        _syncedPhase = 0;
        _syncedProgress01 = 0f;
        _syncedHeat01 = 0f;

        // 温度は環境基準へ戻す
        _syncedTempC = _syncedAmbientTempC;

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("ReleaseOperator");
        StopLocalSession();
        if (sampleVisual != null) sampleVisual.NotifyExperimentReset();
        ApplyVisualFromState(true);
        WriteUI();
    }

    // =====================================================
    // Button-facing API (all cube buttons can call these)
    // =====================================================
    public void SelectElement(string symbolOrFormula)
    {
        if (!EnsureCanControl()) return;

        _localInput = (symbolOrFormula == null) ? "" : symbolOrFormula.Trim();
        _syncedInput = _localInput;

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("SelectElement: " + _localInput);

        // 非同期演出：器具に投入されるように見せる（ローカル即時）
        if (sampleVisual != null)
            sampleVisual.NotifyElementSelected(_localInput);

        ApplyVisualFromState(true);
        WriteUI();
    }

    public void SelectEquipment(string toolId)
    {
        if (!EnsureCanControl()) return;

        _localTool = (toolId == null) ? "" : toolId.Trim();
        _syncedTool = _localTool;

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("SelectEquipment: " + _localTool);
        WriteUI();
    }

    /// <summary>
    /// 環境調整（ConditionAdjusterから呼ぶ想定）
    /// 既存の TempUp/TempDown 等に加えて、HeatUp/HeatDown/HeatOn/HeatOff も受け付ける。
    /// </summary>
    public void ModifyEnvironment(string command)
    {
        if (!EnsureCanControl()) return;
        if (string.IsNullOrEmpty(command)) return;

        // ---- heater commands (synced) ----
        if (command == "HeatOn")
        {
            SetHeat01(1f, true);
            return;
        }
        if (command == "HeatOff")
        {
            SetHeat01(0f, true);
            return;
        }
        if (command == "HeatUp")
        {
            SetHeat01(_syncedHeat01 + 0.1f, true);
            return;
        }
        if (command == "HeatDown")
        {
            SetHeat01(_syncedHeat01 - 0.1f, true);
            return;
        }

        // ---- normal environment commands ----
        if (environment == null) return;

        environment.Modify(command);

        // 同期へ反映（基準温度は環境のTemperature）
        _syncedAmbientTempC = environment.temperatureC;
        _syncedPressureKPa = environment.pressureKPa;
        _syncedHumidity = environment.humidity;

        // idle中は現在温度も追従
        if (_syncedPhase == 0)
        {
            _syncedTempC = _syncedAmbientTempC;
            _simTempC = _syncedTempC;
        }

        RequestSerialization();

        AppendHistory("ModifyEnv: " + command);
        WriteUI();
    }

    // 実験開始（開始ボタン想定）
    public void _StartExperiment()
    {
        if (!EnsureCanControl()) return;

        // 環境値（確定値）を取得して同期に載せる
        PullEnvironmentForSync(false);

        // Seed（再現性）：入力/器具/環境からint範囲ハッシュ
        _syncedSeed = ComputeSeed(_syncedInput, _syncedTool, _syncedAmbientTempC, _syncedPressureKPa, _syncedHumidity);

        // 反応予測（結果の"真実"）
        string product = _syncedInput;
        string tag = "none";
        string explain = "";
        if (predictor != null)
        {
            predictor.Predict(_syncedInput, _syncedTool, out product, out tag, out explain);
        }
        _syncedProductFormula = product;
        _syncedReactionTag = tag;

        // フェーズ更新
        _syncedPhase = 1;
        _syncedProgress01 = 0f;

        // 温度初期化（基準温度スタート、加熱0）
        _syncedHeat01 = 0f;
        _syncedTempC = _syncedAmbientTempC;
        _simTempC = _syncedTempC;
        _visualTempC = _syncedTempC;
        _tempInitialized = true;

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("StartExperiment: input=" + _syncedInput + " tool=" + _syncedTool + " tag=" + _syncedReactionTag);

        // ローカル：セッション開始（視覚/説明生成）
        if (sampleVisual != null) sampleVisual.NotifyExperimentReset();
        StartLocalSessionFromSynced();

        ApplyVisualFromState(true);
        WriteUI();
    }

    public void _ResetExperiment()
    {
        if (!EnsureCanControl()) return;

        // 同期状態初期化
        _syncedInput = "";
        _syncedTool = "";
        _syncedReactionTag = "none";
        _syncedProductFormula = "";
        _syncedSeed = 0;
        _syncedPhase = 0;
        _syncedProgress01 = 0f;
        _syncedHeat01 = 0f;

        // envはデフォルトに戻す（環境マネージャ側でもResetされる想定）
        PullEnvironmentForSync(false);

        // 温度は基準へ戻す
        _syncedTempC = _syncedAmbientTempC;
        _simTempC = _syncedTempC;

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("ResetExperiment");
        StopLocalSession();
        if (sampleVisual != null) sampleVisual.NotifyExperimentReset();
        ApplyVisualFromState(true);
        WriteUI();
    }

    // =====================================================
    // Update loop (async visuals at 60fps, low-rate sync)
    // =====================================================
    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) dt = 0.016f;

        // 温度モデル＆見た目温度補間（60fps）
        TickLocalTemperature(dt);

        // ローカル可視化（60fps）
        TickLocalVisual(dt);

        // 見た目：温度に応じて固液気を切替（非同期）
        ApplyVisualContinuous();

        // フェーズ遷移を検出してローカル演出へ通知（主にcomplete）
        if (_syncedPhase != _lastAppliedPhase)
        {
            if (sampleVisual != null && _syncedPhase == 2)
            {
                sampleVisual.NotifyReactionComplete(_syncedProductFormula, _syncedReactionTag);
            }
            _lastAppliedPhase = _syncedPhase;
        }

        // operatorのみ：進行度を低頻度で同期
        if (_syncedPhase == 1 && IsOperatorLocal())
        {
            if (Time.time >= _nextProgressSyncAt)
            {
                _nextProgressSyncAt = Time.time + Mathf.Max(0.05f, progressSyncInterval);

                // aiがあれば、aiの進行度を同期に乗せる
                if (ai != null && ai.isRunning)
                {
                    _syncedProgress01 = Mathf.Clamp01(ai.sessionProgress01);
                }
                RequestSerialization();
            }
        }

        // operatorのみ：温度を低頻度で同期
        if (_syncedPhase == 1 && IsOperatorLocal() && enableDynamicTemperature)
        {
            if (Time.time >= _nextTempSyncAt)
            {
                _nextTempSyncAt = Time.time + Mathf.Max(0.10f, temperatureSyncInterval);
                _syncedTempC = _simTempC;

                // UI表示（ローカル env を更新）
                if (environment != null) environment.Temperature = _syncedTempC;

                RequestSerialization();
            }
        }
    }

    private void TickLocalVisual(float dt)
    {
        // operator：入力操作で進行（将来：手の動き/注ぎ/加熱を入れる）
        if (_syncedPhase == 1 && IsOperatorLocal())
        {
            if (ai != null)
            {
                // 現状はモーション入力未接続（0）。必要ならここに stir/pour/shake などを足す。
                ai.TickRealtime(dt, 0f, 0f, 0f, 0f);
                if (ai.isComplete)
                {
                    _syncedPhase = 2;
                    _syncedProgress01 = 1f;
                    _syncedVersion++;
                    RequestSerialization();

                    AppendHistory("ExperimentComplete");
                    WriteUI();
                }
            }
        }
        else
        {
            // spectator：同期Progressに追従してローカル演出を動かす
            if (ai != null && _syncedPhase == 1)
            {
                ai.EvaluateAtProgress(_syncedProgress01);
            }
        }

        // 演出適用（ローカル）
        if (reactionAnimator != null && ai != null)
        {
            reactionAnimator.ApplyPreset(_syncedReactionTag, ai, _syncedProgress01);
        }
    }

    /// <summary>
    /// 温度の「同期の真実」と「非同期の見た目」を分離。
    /// - operator：温度をシミュレートし、低頻度で _syncedTempC を更新
    /// - spectator：_syncedTempC を受け取り、_visualTempC を60fpsで補間
    /// </summary>
    private void TickLocalTemperature(float dt)
    {
        if (!_tempInitialized)
        {
            _simTempC = _syncedTempC;
            _visualTempC = _syncedTempC;
            _tempInitialized = true;
        }

        // 見た目温度：同期温度へ追従（全員）
        float lerpT = 1f - Mathf.Exp(-Mathf.Max(0.01f, visualTempLerpSpeed) * dt);
        _visualTempC = Mathf.Lerp(_visualTempC, _syncedTempC, lerpT);

        if (!enableDynamicTemperature) return;
        if (_syncedPhase != 1) return;

        // operatorのみシミュレーション（スペクテイターは _syncedTempC に従う）
        if (!IsOperatorLocal()) return;

        float heat01 = _syncedHeat01;
        if (autoHeatFromProximity)
        {
            float autoHeat = ComputeHeat01FromProximity();

            // 自動ヒートは同期値も追従させる（ただし連打しないため温度同期のタイミングで反映）
            heat01 = autoHeat;
            _syncedHeat01 = autoHeat;
        }

        float reaction01 = 0f;
        if (ai != null) reaction01 = Mathf.Clamp01(ai.fxHeat);

        float target = _syncedAmbientTempC
            + heat01 * externalHeatMaxDeltaC
            + reaction01 * reactionHeatMaxDeltaC;

        float step = Mathf.Max(0.01f, thermalResponseCPerSec) * dt;
        _simTempC = Mathf.MoveTowards(_simTempC, target, step);
    }

    private float ComputeHeat01FromProximity()
    {
        if (containerTransform == null) containerTransform = transform;
        if (heatSource == null) return _syncedHeat01;

        float d = Vector3.Distance(containerTransform.position, heatSource.position);
        if (d <= heatNearMeters) return 1f;
        if (d >= heatFarMeters) return 0f;

        // 近いほど1
        float t = Mathf.InverseLerp(heatFarMeters, heatNearMeters, d);
        return Mathf.Clamp01(t);
    }


    private string GetDisplayFormula()
    {
        if (showProductWhenComplete && _syncedPhase == 2)
        {
            if (!string.IsNullOrEmpty(_syncedProductFormula)) return _syncedProductFormula;
        }
        return string.IsNullOrEmpty(_syncedInput) ? "" : _syncedInput;
    }

    private void ApplyVisualContinuous()
    {
        if (sampleVisual == null || elementDb == null) return;

        string sym = GetDisplayFormula();
        sampleVisual.ApplyElementBySymbol(elementDb, sym, _visualTempC);
    }

    // =====================================================
    // Sync receive
    // =====================================================
    public override void OnDeserialization()
    {
        // 進行度/温度の更新など「軽い変化」も反映したいので、versionガードは重い処理だけに使う。
        bool majorChanged = _syncedVersion != _lastAppliedVersion;

        // ローカルキャッシュ更新
        _localInput = _syncedInput == null ? "" : _syncedInput;
        _localTool = _syncedTool == null ? "" : _syncedTool;

        // UI側の環境表示を更新（常に）
        if (environment != null)
        {
            environment.Temperature = _syncedTempC;
            environment.Pressure = _syncedPressureKPa;
            environment.Humidity = _syncedHumidity;
        }

        if (majorChanged)
        {
            _lastAppliedVersion = _syncedVersion;

            // セッション状態の反映
            if (_syncedPhase == 0)
            {
                StopLocalSession();
            }
            else
            {
                // spectator/後から入った人：同期状態からローカルセッション復元
                StartLocalSessionFromSynced();
            }

            // 温度追従を安定化
            _simTempC = _syncedTempC;
            _visualTempC = _syncedTempC;
            _tempInitialized = true;

            ApplyVisualFromState(true);
        }

        // 軽い更新（progress/temp等）はここでUI更新
        WriteUI();
    }

    // =====================================================
    // Helpers
    // =====================================================
    private void PullEnvironmentForSync(bool initIfUnset)
    {
        if (environment != null)
        {
            _syncedAmbientTempC = environment.temperatureC;
            _syncedPressureKPa = environment.pressureKPa;
            _syncedHumidity = environment.humidity;

            if (_syncedPhase == 0)
                _syncedTempC = _syncedAmbientTempC;

            return;
        }

        // environment が無い場合の初期値
        if (initIfUnset)
        {
            if (_syncedAmbientTempC == 0f && _syncedTempC == 0f)
            {
                _syncedAmbientTempC = 25f;
                _syncedTempC = 25f;
                _syncedPressureKPa = 101f;
                _syncedHumidity = 40f;
            }
        }
    }

    private void SetHeat01(float value01, bool recordHistory)
    {
        _syncedHeat01 = Mathf.Clamp01(value01);
        RequestSerialization();

        if (recordHistory)
        {
            AppendHistory("Heat01=" + _syncedHeat01.ToString("0.00"));
            WriteUI();
        }
    }

    private void StartLocalSessionFromSynced()
    {
        if (ai != null)
        {
            ai.useOverrideSeed = true;
            ai.sessionSeedOverride = _syncedSeed;
            ai.StartSession(_syncedInput, _syncedTool);

            // spectatorは外部progressで追従させる
            if (!IsOperatorLocal())
            {
                ai.EvaluateAtProgress(_syncedProgress01);
            }
        }

        // 説明/ヒント/安全（決定論）
        if (explainGenerator != null)
        {
            float potential = (_syncedReactionTag == "none") ? 0.1f : 0.8f;
            bool dangerous = (elementDb != null && elementDb.GetHazard(_syncedInput) != 0);

            string hint, explain, safety;
            explainGenerator.Generate(_syncedInput, _syncedTool, potential, _syncedAmbientTempC, _syncedPressureKPa, _syncedHumidity, dangerous,
                out hint, out explain, out safety);

            if (hintText != null) hintText.text = hint;
            if (explainText != null) explainText.text = explain;
            if (safetyText != null) safetyText.text = safety;
        }
    }

    private void StopLocalSession()
    {
        if (ai != null) ai.ResetSession();
        if (reactionAnimator != null) reactionAnimator.ResetLevels();
    }

    private void ApplyVisualFromState(bool force)
    {
        if (sampleVisual == null || elementDb == null) return;

        string sym = GetDisplayFormula();
        sampleVisual.ApplyElementBySymbol(elementDb, sym, _visualTempC);

        // 完了フェーズ通知（ローカル演出）
        if (_syncedPhase != _lastAppliedPhase)
        {
            if (sampleVisual != null && _syncedPhase == 2)
            {
                sampleVisual.NotifyReactionComplete(_syncedProductFormula, _syncedReactionTag);
            }
            _lastAppliedPhase = _syncedPhase;
        }
    }

    private void WriteUI()
    {
        if (debugText == null) return;

        string role = IsOperatorLocal() ? "Operator" : "Spectator";

        debugText.text =
            "Role: " + role + "\n" +
            "OperatorId: " + _syncedOperatorPlayerId + "\n" +
            "Input: " + (_syncedInput ?? "") + "\n" +
            "Tool: " + (_syncedTool ?? "") + "\n" +
            "Phase: " + _syncedPhase + " (" + Mathf.RoundToInt(_syncedProgress01 * 100f) + "%)\n" +
            "Temp: " + _syncedTempC.ToString("0.0") + " °C (ambient " + _syncedAmbientTempC.ToString("0.0") + ")\n" +
            "Heat01: " + _syncedHeat01.ToString("0.00") + "\n" +
            "Reaction: " + (_syncedReactionTag ?? "none") + "\n" +
            "Product: " + (_syncedProductFormula ?? "") + "\n";
    }

    private void AppendHistory(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        _history += line + "\n";
    }

    private void AppendDebug(string line)
    {
        if (debugText == null) return;
        debugText.text = (debugText.text ?? "") + "\n" + line;
    }

    // Udon互換：int内で回る簡易ハッシュ
    private int ComputeSeed(string a, string b, float tC, float pKPa, float h)
    {
        int hash = 17;
        hash = HashStep(hash, a);
        hash = HashStep(hash, b);
        hash = HashStep(hash, Mathf.RoundToInt(tC * 10f).ToString());
        hash = HashStep(hash, Mathf.RoundToInt(pKPa * 10f).ToString());
        hash = HashStep(hash, Mathf.RoundToInt(h * 100f).ToString());
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

    // =====================================================
    // Public getters (used by status UI)
    // =====================================================
    public string GetLastElement()
    {
        return _syncedInput == null ? "" : _syncedInput;
    }

    public string GetLastEquipment()
    {
        return _syncedTool == null ? "" : _syncedTool;
    }

    public string GetHistoryLog()
    {
        return _history == null ? "" : _history;
    }

    public float GetCurrentTemperatureC()
    {
        return _visualTempC;
    }


    public string GetInputFormula()
    {
        return _syncedInput == null ? "" : _syncedInput;
    }

    public string GetProductFormula()
    {
        return _syncedProductFormula == null ? "" : _syncedProductFormula;
    }

    public string GetDisplayFormulaForUI()
    {
        return GetDisplayFormula();
    }

    public int GetPhase()
    {
        return _syncedPhase;
    }

    public float GetProgress01()
    {
        return _syncedProgress01;
    }

    public float GetHeat01()
    {
        return _syncedHeat01;
    }

    public float GetSyncedTemperatureC()
    {
        return _syncedTempC;
    }

    public float GetAmbientTemperatureC()
    {
        return _syncedAmbientTempC;
    }

    public string GetReactionTag()
    {
        return _syncedReactionTag == null ? "none" : _syncedReactionTag;
    }
}
