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
    public AIRequestSender ai;

    [Header("Education/Game Flow (optional)")]
    public ExperimentOrchestrator orchestrator;
                      // VFX係数生成（ローカル）

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


    // =====================================================
    // 3D Preview (Tool + In-Tool Element)
    // - Tool button: show the selected tool model
    // - Element button: generate visual inside the selected tool (anchor)
    // =====================================================
    [Header("3D Preview (Tool + In-Tool Element)")]
    [Tooltip("Tool model root. Direct children are treated as tool candidates (name must match toolId).")]
    public Transform toolModelsRoot;

    [Tooltip("If true, hide tools other than the selected one.")]
    public bool hideOtherTools = true;

    [Tooltip("Anchor name inside the tool where the element visual will be placed.")]
    public string elementEffectAnchorName = "ElementEffectAnchor";

    [Tooltip("If the selected tool has no anchor, use this fallback anchor. If null, containerTransform is used.")]
    public Transform elementEffectAnchorFallback;

    [Tooltip("If true, selecting an element will place the element visual inside the selected tool.")]
    public bool showElementInToolOnSelect = true;

    [Tooltip("Reparent reactionAnimator under the anchor as well (so particles appear inside the tool).")]
    public bool reparentVfxToAnchor = true;

    [Header("Select Preview VFX (AI, optional)")]
    [Tooltip("Run a quick 'AI-like' preview when selecting an element (idle phase only).")]
    public bool previewVfxOnSelectElement = true;

    [Range(0f, 1f)]
    public float previewVfxProgress01 = 0.25f;

    [UdonSynced] private int _syncedVersion;              // 主要変更カウンタ（選択/開始/リセット/操作者変更）
    [UdonSynced]     private int _localLastPhase = -99;

    private int _syncedOperatorPlayerId = -1;

    [UdonSynced] private string _syncedInput;            // 元素/式（ボタンのidOrName）
    [UdonSynced] private string _syncedTool;             // 器具ID

    // 環境（同期）
    [UdonSynced] private float _syncedTempC;             // 現在温度（実験中は動的に更新）
    [UdonSynced] private float _syncedAmbientTempC;      // 基準温度（初期条件/環境設定）
    [UdonSynced] private float _syncedPressureKPa;
    [UdonSynced] private float _syncedHumidity;

    // 温度操作（同期）
    [UdonSynced] private float _syncedHeat01;            // 0..1 外部加熱レベル（ボタン or 自動近接）

    // 操作（同期）：教材ゲームの採点対象にもする
    // 「現在値」と「実験中に到達した最大値」を分ける（採点で“やったか”を見るため）
    [UdonSynced] private float _syncedStir01;
    [UdonSynced] private float _syncedPour01;
    [UdonSynced] private float _syncedShake01;

    [UdonSynced] private float _syncedMaxHeat01;
    [UdonSynced] private float _syncedMaxStir01;
    [UdonSynced] private float _syncedMaxPour01;
    [UdonSynced] private float _syncedMaxShake01;

    [UdonSynced] private float _syncedMinTempCReached;
    [UdonSynced] private float _syncedMaxTempCReached;

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


// 3D preview local cache
private string _lastAppliedToolId = "";
private string _lastAppliedElement = "";
private Transform _activeTool;
private Transform _activeAnchor;

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
    
        _localLastPhase = _syncedPhase;
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
        _syncedStir01 = 0f;
        _syncedPour01 = 0f;
        _syncedShake01 = 0f;

        _syncedMaxHeat01 = 0f;
        _syncedMaxStir01 = 0f;
        _syncedMaxPour01 = 0f;
        _syncedMaxShake01 = 0f;

        // 温度は環境基準へ戻す
        _syncedTempC = _syncedAmbientTempC;
        _syncedMinTempCReached = _syncedTempC;
        _syncedMaxTempCReached = _syncedTempC;

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
    
        // 3D preview (local immediate)
        ApplyToolSelectionLocal(true);
        ApplyElementSelectionLocal(true);
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
    
        // 3D preview (local immediate)
        ApplyToolSelectionLocal(true);
        // If element already selected, keep it inside the newly selected tool
        ApplyElementSelectionLocal(true);
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

        // ---- operation commands (synced) ----
        // Stir
        if (command == "StirOn") { SetStir01(1f, true); return; }
        if (command == "StirOff") { SetStir01(0f, true); return; }
        if (command == "StirUp") { SetStir01(_syncedStir01 + 0.1f, true); return; }
        if (command == "StirDown") { SetStir01(_syncedStir01 - 0.1f, true); return; }

        // Pour
        if (command == "PourOn") { SetPour01(1f, true); return; }
        if (command == "PourOff") { SetPour01(0f, true); return; }
        if (command == "PourUp") { SetPour01(_syncedPour01 + 0.1f, true); return; }
        if (command == "PourDown") { SetPour01(_syncedPour01 - 0.1f, true); return; }

        // Shake
        if (command == "ShakeOn") { SetShake01(1f, true); return; }
        if (command == "ShakeOff") { SetShake01(0f, true); return; }
        if (command == "ShakeUp") { SetShake01(_syncedShake01 + 0.1f, true); return; }
        if (command == "ShakeDown") { SetShake01(_syncedShake01 - 0.1f, true); return; }

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
        _syncedStir01 = 0f;
        _syncedPour01 = 0f;
        _syncedShake01 = 0f;

        _syncedMaxHeat01 = 0f;
        _syncedMaxStir01 = 0f;
        _syncedMaxPour01 = 0f;
        _syncedMaxShake01 = 0f;
        _syncedTempC = _syncedAmbientTempC;
        _simTempC = _syncedTempC;
        _visualTempC = _syncedTempC;
        _tempInitialized = true;

        _syncedMinTempCReached = _syncedTempC;
        _syncedMaxTempCReached = _syncedTempC;

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
        _syncedStir01 = 0f;
        _syncedPour01 = 0f;
        _syncedShake01 = 0f;

        _syncedMaxHeat01 = 0f;
        _syncedMaxStir01 = 0f;
        _syncedMaxPour01 = 0f;
        _syncedMaxShake01 = 0f;

        // envはデフォルトに戻す（環境マネージャ側でもResetされる想定）
        PullEnvironmentForSync(false);

        // 温度は基準へ戻す
        _syncedTempC = _syncedAmbientTempC;
        _simTempC = _syncedTempC;

        _syncedMinTempCReached = _syncedTempC;
        _syncedMaxTempCReached = _syncedTempC;

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

        // operatorのみ：実験中の到達値を記録（採点用）
        if (_syncedPhase == 1 && IsOperatorLocal())
        {
            if (_syncedHeat01 > _syncedMaxHeat01) _syncedMaxHeat01 = _syncedHeat01;
            if (_syncedStir01 > _syncedMaxStir01) _syncedMaxStir01 = _syncedStir01;
            if (_syncedPour01 > _syncedMaxPour01) _syncedMaxPour01 = _syncedPour01;
            if (_syncedShake01 > _syncedMaxShake01) _syncedMaxShake01 = _syncedShake01;

            // 温度の到達レンジ（シミュレート温度）
            if (_simTempC < _syncedMinTempCReached) _syncedMinTempCReached = _simTempC;
            if (_simTempC > _syncedMaxTempCReached) _syncedMaxTempCReached = _simTempC;
        }

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
    
        CheckPhaseTransition();
}

    private void TickLocalVisual(float dt)
    {
        // operator：入力操作で進行（将来：手の動き/注ぎ/加熱を入れる）
        if (_syncedPhase == 1 && IsOperatorLocal())
        {
            if (ai != null)
            {
                // 操作入力（同期）：教材ゲームの採点でも使用
                // TickRealtime(dt, stir, pour, heat, shake)
                ai.TickRealtime(dt, _syncedStir01, _syncedPour01, _syncedHeat01, _syncedShake01);
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
            reactionAnimator.ApplyPreset(_syncedReactionTag, ai, _syncedProgress01, sampleVisual);
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
    
        
        // 3D preview (sync apply)
        ApplyToolSelectionLocal(true);
        ApplyElementSelectionLocal(true);

CheckPhaseTransition();
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
        if (_syncedHeat01 > _syncedMaxHeat01) _syncedMaxHeat01 = _syncedHeat01;
        RequestSerialization();

        if (recordHistory)
        {
            AppendHistory("Heat01=" + _syncedHeat01.ToString("0.00"));
            WriteUI();
        }
    }

    private void SetStir01(float value01, bool recordHistory)
    {
        _syncedStir01 = Mathf.Clamp01(value01);
        if (_syncedStir01 > _syncedMaxStir01) _syncedMaxStir01 = _syncedStir01;
        RequestSerialization();

        if (recordHistory)
        {
            AppendHistory("Stir01=" + _syncedStir01.ToString("0.00"));
            WriteUI();
        }
    }

    private void SetPour01(float value01, bool recordHistory)
    {
        _syncedPour01 = Mathf.Clamp01(value01);
        if (_syncedPour01 > _syncedMaxPour01) _syncedMaxPour01 = _syncedPour01;
        RequestSerialization();

        if (recordHistory)
        {
            AppendHistory("Pour01=" + _syncedPour01.ToString("0.00"));
            WriteUI();
        }
    }

    private void SetShake01(float value01, bool recordHistory)
    {
        _syncedShake01 = Mathf.Clamp01(value01);
        if (_syncedShake01 > _syncedMaxShake01) _syncedMaxShake01 = _syncedShake01;
        RequestSerialization();

        if (recordHistory)
        {
            AppendHistory("Shake01=" + _syncedShake01.ToString("0.00"));
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
            // 反応らしさ（簡易）: 操作量と反応タグから算出
            float op = Mathf.Clamp01(0.25f * _syncedStir01 + 0.25f * _syncedPour01 + 0.35f * _syncedHeat01 + 0.15f * _syncedShake01);
            float potential = (_syncedReactionTag == "none") ? (0.10f + 0.40f * op) : (0.60f + 0.60f * op);

            bool dangerous = (elementDb != null && elementDb.GetHazardForFormulaOrElement(_syncedInput) != 0);
            bool known = (elementDb != null) && (elementDb.ContainsSymbol(_syncedInput) || elementDb.ContainsCompound(_syncedInput));

            // 推定メモ（UI用）
            string note = "";
            if (!known)
            {
                note = (sampleVisual != null && !string.IsNullOrEmpty(sampleVisual.lastInferenceNote))
                    ? sampleVisual.lastInferenceNote
                    : "組成から推定して見た目を生成（外部APIなし）";
            }

            string hint, explain, safety;
            explainGenerator.GenerateDetailed(
                _syncedInput,
                _syncedTool,
                _syncedReactionTag,
                _syncedStir01,
                _syncedPour01,
                _syncedHeat01,
                _syncedShake01,
                _syncedTempC,
                _syncedPressureKPa,
                _syncedHumidity,
                dangerous,
                known,
                note,
                out hint,
                out explain,
                out safety
            );

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


    // =====================================================
    // 3D Preview helpers (Udon-safe: no tag search, no recursion)
    // =====================================================
    private void ApplyToolSelectionLocal(bool force)
    {
        if (toolModelsRoot == null) return;

        string id = _syncedTool;
        if (string.IsNullOrEmpty(id)) id = _localTool;
        if (id == null) id = "";

        if (!force && id == _lastAppliedToolId) return;
        _lastAppliedToolId = id;

        string norm = NormalizeId(id);

        // Pass 1: find match
        Transform matched = null;
        int c = toolModelsRoot.childCount;
        if (!string.IsNullOrEmpty(norm))
        {
            for (int i = 0; i < c; i++)
            {
                Transform child = toolModelsRoot.GetChild(i);
                if (child == null) continue;
                if (NormalizeId(child.name) == norm)
                {
                    matched = child;
                    break;
                }
            }
        }

        // Pass 2: apply visibility
        if (hideOtherTools)
        {
            for (int i = 0; i < c; i++)
            {
                Transform child = toolModelsRoot.GetChild(i);
                if (child == null) continue;

                bool active = true;
                if (!string.IsNullOrEmpty(norm))
                    active = (child == matched); // if not found, keep all visible

                child.gameObject.SetActive(active);
            }
        }

        _activeTool = matched;
    }

    private void ApplyElementSelectionLocal(bool force)
    {
        if (!showElementInToolOnSelect) return;
        if (sampleVisual == null) return;

        string elem = _syncedInput;
        if (string.IsNullOrEmpty(elem)) elem = _localInput;
        if (elem == null) elem = "";

        if (!force && elem == _lastAppliedElement && _activeAnchor != null) return;
        _lastAppliedElement = elem;

        // Make sure tool selection is applied before resolving anchor
        ApplyToolSelectionLocal(false);

        _activeAnchor = ResolveAnchor();
        if (_activeAnchor == null) return;

        // Place sampleVisual inside the tool/anchor
        Transform svt = sampleVisual.transform;
        Vector3 prevLocalScale = svt.localScale;

        if (svt.parent != _activeAnchor)
            svt.SetParent(_activeAnchor, false);

        svt.localPosition = Vector3.zero;
        svt.localRotation = Quaternion.identity;
        svt.localScale = prevLocalScale;

        if (!sampleVisual.gameObject.activeSelf)
            sampleVisual.gameObject.SetActive(true);

        // Generate "real-ish" look (color/state/opacity/etc.) using existing deterministic systems
        if (elementDb != null)
            sampleVisual.ApplyElementBySymbol(elementDb, elem, _visualTempC);

        // Keep VFX origin inside the tool, too
        if (reparentVfxToAnchor && reactionAnimator != null)
        {
            Transform rt = reactionAnimator.transform;
            Vector3 rScale = rt.localScale;

            if (rt.parent != _activeAnchor)
                rt.SetParent(_activeAnchor, false);

            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = rScale;
        }

        // Optional: quick AI-like preview when selecting an element (idle only)
        if (previewVfxOnSelectElement && _syncedPhase == 0 && ai != null && reactionAnimator != null)
        {
            string toolId = _syncedTool;
            if (string.IsNullOrEmpty(toolId)) toolId = _localTool;
            if (toolId == null) toolId = "";

            int seed = ComputeSeedCompat(elem, toolId);

            bool prevOverride = ai.useOverrideSeed;
            int prevSeed = ai.sessionSeedOverride;

            ai.useOverrideSeed = true;
            ai.sessionSeedOverride = seed;

            ai.StartSession(elem, toolId);
            ai.EvaluateAtProgress(previewVfxProgress01);

            reactionAnimator.ApplyPreset(ai.predictedReactionTag, ai, previewVfxProgress01, sampleVisual);

            ai.ResetSession();

            ai.useOverrideSeed = prevOverride;
            ai.sessionSeedOverride = prevSeed;
        }
    }

    private Transform ResolveAnchor()
    {
        // Prefer anchor inside selected tool
        if (_activeTool != null)
        {
            Transform a = FindChildByNameDepth3(_activeTool, elementEffectAnchorName);
            if (a != null) return a;

            // If no explicit anchor, use the tool root itself
            return _activeTool;
        }

        // Fallback anchors
        if (elementEffectAnchorFallback != null) return elementEffectAnchorFallback;
        if (containerTransform != null) return containerTransform;
        return transform;
    }

    private Transform FindChildByNameDepth3(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return null;

        // depth 1
        int c1 = root.childCount;
        for (int i = 0; i < c1; i++)
        {
            Transform ch = root.GetChild(i);
            if (ch != null && ch.name == targetName) return ch;
        }

        // depth 2
        for (int i = 0; i < c1; i++)
        {
            Transform ch = root.GetChild(i);
            if (ch == null) continue;
            int c2 = ch.childCount;
            for (int j = 0; j < c2; j++)
            {
                Transform g = ch.GetChild(j);
                if (g != null && g.name == targetName) return g;
            }
        }

        // depth 3
        for (int i = 0; i < c1; i++)
        {
            Transform ch = root.GetChild(i);
            if (ch == null) continue;
            int c2 = ch.childCount;
            for (int j = 0; j < c2; j++)
            {
                Transform g = ch.GetChild(j);
                if (g == null) continue;
                int c3 = g.childCount;
                for (int k = 0; k < c3; k++)
                {
                    Transform gg = g.GetChild(k);
                    if (gg != null && gg.name == targetName) return gg;
                }
            }
        }

        return null;
    }

    private string NormalizeId(string s)
    {
        if (s == null) return "";

        // Remove spaces/underscores/hyphens and uppercase (ASCII)
        string outStr = "";
        int len = s.Length;
        for (int i = 0; i < len; i++)
        {
            char c = s[i];
            if (c == ' ' || c == '\t' || c == '\n' || c == '_' || c == '-') continue;

            // ASCII upper
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);

            outStr += c;
        }
        return outStr;
    }

    private int ComputeSeedCompat(string a, string b)
    {
        int h = 17;
        h = (h * 31) + HashCompat(a);
        h = (h * 31) + HashCompat(b);
        if (h < 0) h = -h;
        return h;
    }

    private int HashCompat(string s)
    {
        if (s == null) return 0;
        int hash = 0;
        int len = s.Length;
        for (int i = 0; i < len; i++)
        {
            hash = (hash * 31) + (int)s[i];
        }
        return hash;
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
            "Stir01: " + _syncedStir01.ToString("0.00") + " (max " + _syncedMaxStir01.ToString("0.00") + ")\n" +
            "Pour01: " + _syncedPour01.ToString("0.00") + " (max " + _syncedMaxPour01.ToString("0.00") + ")\n" +
            "Shake01: " + _syncedShake01.ToString("0.00") + " (max " + _syncedMaxShake01.ToString("0.00") + ")\n" +
            "TempReached: " + _syncedMinTempCReached.ToString("0.0") + ".." + _syncedMaxTempCReached.ToString("0.0") + " °C\n" +
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

    public float GetStir01() { return _syncedStir01; }
    public float GetPour01() { return _syncedPour01; }
    public float GetShake01() { return _syncedShake01; }

    // =====================================================
    // Public ops setter (VR/physical input)
    // =====================================================
    // VRなどから連続値(0..1)で操作量を直接渡すための入口
    public void SetOps01(float heat01, float stir01, float pour01, float shake01)
    {
        if (!EnsureCanControl()) return;

        heat01  = Mathf.Clamp01(heat01);
        stir01  = Mathf.Clamp01(stir01);
        pour01  = Mathf.Clamp01(pour01);
        shake01 = Mathf.Clamp01(shake01);

        // 同期負荷軽減：僅差は無視
        const float eps = 0.02f;
        bool changed =
            Mathf.Abs(_syncedHeat01  - heat01)  > eps ||
            Mathf.Abs(_syncedStir01  - stir01)  > eps ||
            Mathf.Abs(_syncedPour01  - pour01)  > eps ||
            Mathf.Abs(_syncedShake01 - shake01) > eps;

        if (!changed) return;

        _syncedHeat01  = heat01;
        _syncedStir01  = stir01;
        _syncedPour01  = pour01;
        _syncedShake01 = shake01;

        // 最大値（採点用）も更新
        if (_syncedHeat01  > _syncedMaxHeat01)  _syncedMaxHeat01  = _syncedHeat01;
        if (_syncedStir01  > _syncedMaxStir01)  _syncedMaxStir01  = _syncedStir01;
        if (_syncedPour01  > _syncedMaxPour01)  _syncedMaxPour01  = _syncedPour01;
        if (_syncedShake01 > _syncedMaxShake01) _syncedMaxShake01 = _syncedShake01;

        RequestSerialization();
    }

    public float GetMaxHeat01() { return _syncedMaxHeat01; }
    public float GetMaxStir01() { return _syncedMaxStir01; }
    public float GetMaxPour01() { return _syncedMaxPour01; }
    public float GetMaxShake01() { return _syncedMaxShake01; }

    public float GetMinTempReachedC() { return _syncedMinTempCReached; }
    public float GetMaxTempReachedC() { return _syncedMaxTempCReached; }

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

    private void CheckPhaseTransition()
    {
        if (_localLastPhase == _syncedPhase) return;

        // Notify orchestrator about phase change (local event)
        if (orchestrator != null)
        {
            orchestrator.SendCustomEvent("_OnSpawnerPhaseChanged");
            if (_syncedPhase == 2) // complete
            {
                orchestrator.SendCustomEvent("_OnExperimentCompleted");
            }
        }

        _localLastPhase = _syncedPhase;
    }

}