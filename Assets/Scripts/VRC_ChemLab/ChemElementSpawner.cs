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


    [Header("3D Preview / Placement (Safe)")]
    [Tooltip("器具モデル群の親（推奨: World/ExperimentTable/VR_Props）。UIボード(=Tool)を入れないでください。")]
    public Transform toolModelsRoot;

    [Tooltip("元素を器具内に配置するアンカー名（器具モデルの子に置く）。")]
    public string elementEffectAnchorName = "ElementEffectAnchor";

    [Tooltip("アンカーが見つからない場合の代替（未設定なら containerTransform）。")]
    public Transform elementEffectAnchorFallback;

    [Tooltip("元素選択時に、sampleVisual を器具内アンカーへ移動して表示します。")]
    public bool placeElementEffectInTool = true;

    [Tooltip("器具選択時に、toolModelsRoot 配下の一致する器具をアクティブ化します（VR_Props のときのみ安全に他を非表示にできます）。")]
    public bool previewToolOnSelect = true;

    [Tooltip("VR_Props のときだけ、選択器具以外を非表示にします（UIボード消失対策）。")]
    public bool hideOtherToolsOnlyWhenVRProps = true;

    [Tooltip("sampleVisual の表示オフセット（アンカーのローカル）。")]
    public Vector3 elementEffectLocalOffset = Vector3.zero;

    [Tooltip("sampleVisual の表示スケール（アンカーのローカル）。")]
    public Vector3 elementEffectLocalScale = Vector3.one;


    [Header("Auto BEAKER on element select")]
    [Tooltip("元素ボタン押下時、器具未選択なら自動で BEAKER を選択して表示します。")]
    public bool autoSpawnBeakerOnElement = true;

    [Tooltip("自動選択する器具ID（toolModelsRoot配下の名前に部分一致させます）。例: BEAKER")]
    public string autoBeakerToolId = "BEAKER";

    [Tooltip("自動選択したBEAKERを containerTransform(VR_StartZone) の位置に移動します（親子付けは変えません）。")]
    public bool autoPlaceBeakerAtContainer = true;

    [Tooltip("BEAKERを containerTransform に置く際のワールドオフセット。")]
    public Vector3 autoBeakerWorldOffset = new Vector3(0f, 0.02f, 0f);

    [Header("Force visibility (debug-safe)")]
    [Tooltip("選択時に、BEAKER/SampleVisual の Renderer/Particle/Layer を強制的に可視化します。")]
    public bool forceVisibleOnSelect = true;

    [Tooltip("強制的に設定するLayer。0=Default。")]
    public int forceVisibleLayer = 0;

    private Transform _activeToolTr;
    private Transform _activeToolTopTr;
    private Transform _activeAnchorTr;
    private string _lastToolApplied = "";
    private int _tmpScanVisited = 0;

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

    // -----------------------------
    // Synced experiment "truth"
    // -----------------------------
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


        // 3D placement: do not touch element table UI
        ApplyToolPreviewLocal(false);
        PlaceElementEffectLocal(false);
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
        EnsureAutoBeakerOnElement();
        // Ensure 3D placement updates immediately for element selection
        ApplyToolPreviewLocal(false);
        PlaceElementEffectLocal(true);
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


        ApplyToolPreviewLocal(true);
        PlaceElementEffectLocal(true);
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

        // Ensure previews stay visible for late joiners / remote updates
        ApplyToolPreviewLocal(false);
        PlaceElementEffectLocal(false);

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

    // =====================================================
    // 3D Preview helpers (UI-safe)
    // =====================================================
    private void EnsurePreviewRefs()
    {
        if (containerTransform == null) containerTransform = transform;
        if (elementEffectAnchorFallback == null) elementEffectAnchorFallback = containerTransform;

        // If incorrectly assigned to UI, clear it
        if (toolModelsRoot != null && IsLikelyUIRoot(toolModelsRoot))
            toolModelsRoot = null;

        if (toolModelsRoot != null) return;

        // 1) Try common names / paths
        Transform found = null;

        found = FindByNameAnywhere(containerTransform, "VR_Props");
        if (found == null) found = FindByNameAnywhere(containerTransform, "VRProps");
        if (found == null) found = FindByNameAnywhere(containerTransform, "VR Props");
        if (found == null) found = FindByNameAnywhere(containerTransform, "Props");
        if (found == null) found = FindByNameContainsAnywhere2(containerTransform, "VR", "PROPS");
        if (found == null) found = FindByNameContainsAnywhere1(containerTransform, "PROP");

        // 2) Heuristic: pick a non-UI root with many renderers (limited scan)
        if (found == null)
            found = FindBestPropsRootByRenderers(containerTransform.root);

        if (found != null && !IsLikelyUIRoot(found))
            toolModelsRoot = found;
    }

    private bool IsVRPropsRoot(Transform root)
    {
        if (root == null) return false;
        string n = root.name;
        if (string.IsNullOrEmpty(n)) return false;
        n = n.ToUpper();
        return n == "VR_PROPS" || n == "VRPROPS" || n.Contains("VR_PROPS") || n.Contains("VRPROPS");
    }

    private void ApplyToolPreviewLocal(bool force)
    {
        if (!previewToolOnSelect) return;

        EnsurePreviewRefs();
        if (toolModelsRoot == null) return;

        string toolId = string.IsNullOrEmpty(_syncedTool) ? _localTool : _syncedTool;
        if (toolId == null) toolId = "";
        string norm = NormalizeId(toolId);

        if (!force && norm == _lastToolApplied) return;
        _lastToolApplied = norm;

        // Find best tool under toolModelsRoot (VR_Props recommended)
        _activeToolTr = FindBestToolTransformUnderRoot(toolModelsRoot, norm);
        _activeToolTopTr = GetTopChildUnderRoot(_activeToolTr, toolModelsRoot);

        int c = toolModelsRoot.childCount;
        bool canHideOthers = hideOtherToolsOnlyWhenVRProps && IsVRPropsRoot(toolModelsRoot);

        // If we can safely hide others (only real 3D props root), do it.
        if (canHideOthers && !string.IsNullOrEmpty(norm))
        {
            for (int i = 0; i < c; i++)
            {
                Transform ch = toolModelsRoot.GetChild(i);
                if (ch == null) continue;
                if (!HasAnyRenderer(ch)) continue;

                bool isActive = (_activeToolTopTr != null && ch == _activeToolTopTr);
                if (ch.gameObject.activeSelf != isActive)
                    ch.gameObject.SetActive(isActive);
            }
        }

        // If nothing matched, restore visibility (avoid hiding boards)
        if (_activeToolTr == null && canHideOthers)
        {
            for (int i = 0; i < c; i++)
            {
                Transform ch = toolModelsRoot.GetChild(i);
                if (ch == null) continue;
                if (HasAnyRenderer(ch) && !ch.gameObject.activeSelf) ch.gameObject.SetActive(true);
            }
        }
    }

    private void PlaceElementEffectLocal(bool force)
    {
        if (!placeElementEffectInTool) return;

        EnsurePreviewRefs();
        if (sampleVisual == null) return;

        string sym = string.IsNullOrEmpty(_syncedInput) ? _localInput : _syncedInput;
        if (sym == null) sym = "";

        // If no tool selected, still place at containerTransform
        _activeAnchorTr = ResolveAnchor();

        if (_activeAnchorTr == null) return;

        // Make sure sampleVisual is visible and positioned correctly
        GameObject svGo = sampleVisual.gameObject;
        if (!svGo.activeSelf) svGo.SetActive(true);

        Transform svT = sampleVisual.transform;
        if (svT.parent != _activeAnchorTr)
            svT.SetParent(_activeAnchorTr, false);

        svT.localPosition = elementEffectLocalOffset;
        svT.localRotation = Quaternion.identity;
        svT.localScale = elementEffectLocalScale;

        EnableAllRenderers(svGo);
        if (forceVisibleOnSelect)
        {
            ForceVisibleHierarchy(svGo.transform);
            if (_activeToolTr != null) ForceVisibleHierarchy(_activeToolTr);
        }

        // Apply visual state (color/state) from DB if possible
        if (elementDb != null)
        {
            sampleVisual.ApplyElementBySymbol(elementDb, GetDisplayFormula(), _visualTempC);
        }

        // Keep the element table / button board intact: we never disable it here.
    }

    private Transform ResolveAnchor()
    {
        EnsurePreviewRefs();

        // Resolve active tool if not yet resolved
        if (_activeToolTr == null)
        {
            string toolId = string.IsNullOrEmpty(_syncedTool) ? _localTool : _syncedTool;
            if (toolId == null) toolId = "";
            string norm = NormalizeId(toolId);

            if (toolModelsRoot != null && !string.IsNullOrEmpty(norm))
            {
                _activeToolTr = FindBestToolTransformUnderRoot(toolModelsRoot, norm);
            }
        }

        // Prefer tool's ElementEffectAnchor
        if (_activeToolTr != null)
        {
            Transform a = FindChildByName(_activeToolTr, elementEffectAnchorName, 6);
            if (a != null) return a;
            return _activeToolTr;
        }

        if (elementEffectAnchorFallback != null && !IsUiLikeTransform(elementEffectAnchorFallback)) return elementEffectAnchorFallback;
        if (containerTransform != null) return containerTransform;
        return transform;
    }

    private Transform FindChildByName(Transform root, string targetName, int maxDepth)
    {
        if (root == null || string.IsNullOrEmpty(targetName) || maxDepth < 0) return null;

        int c = root.childCount;
        for (int i = 0; i < c; i++)
        {
            Transform ch = root.GetChild(i);
            if (ch != null && ch.name == targetName) return ch;
        }

        if (maxDepth == 0) return null;

        for (int i = 0; i < c; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName, maxDepth - 1);
            if (found != null) return found;
        }
        return null;
    }

    private bool HasAnyRenderer(Transform tr)
    {
        return CountRenderersUnder(tr, 6, 256) > 0;
    }

    private void EnsureAutoBeakerOnElement()
    {
        if (!autoSpawnBeakerOnElement) return;

        // If tool already selected, do nothing
        bool hasTool = !string.IsNullOrEmpty(_syncedTool) || !string.IsNullOrEmpty(_localTool);
        if (!hasTool)
        {
            _localTool = autoBeakerToolId;
            _syncedTool = autoBeakerToolId;
        }

        // Ensure toolModelsRoot chain is active
        if (toolModelsRoot != null) ActivateParents(toolModelsRoot);

        // Resolve BEAKER transform and activate it
        if (toolModelsRoot != null && !string.IsNullOrEmpty(autoBeakerToolId))
        {
            string norm = NormalizeId(autoBeakerToolId);
            _activeToolTr = FindBestToolTransformUnderRoot(toolModelsRoot, norm);
            if (_activeToolTr != null)
            {
                ActivateParents(_activeToolTr);
                if (!_activeToolTr.gameObject.activeSelf) _activeToolTr.gameObject.SetActive(true);

                if (autoPlaceBeakerAtContainer && containerTransform != null)
                {
                    _activeToolTr.position = containerTransform.position + autoBeakerWorldOffset;
                    _activeToolTr.rotation = containerTransform.rotation;
                }
            }
        }
    }

    private void ActivateParents(Transform tr)
    {
        Transform t = tr;
        int guard = 64;
        while (t != null && guard-- > 0)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    private bool IsUiLikeTransform(Transform tr)
    {
        if (tr == null) return false;

        int layer = tr.gameObject.layer;
        // Typical UI layer is 5; project-specific UI layer might be 13
        if (layer == 5 || layer == 13) return true;

        // RectTransform present => UI
        RectTransform rt = tr.GetComponent<RectTransform>();
        if (rt != null) return true;

        string n = tr.name;
        if (n == null) n = "";
        string ln = n.ToLower();
        if (ln.IndexOf("button") >= 0) return true;
        if (ln.IndexOf("selector") >= 0) return true;
        if (ln.IndexOf("ui") >= 0) return true;
        if (ln.IndexOf("periodic") >= 0) return true;

        // Under a UI root?
        Transform p = tr.parent;
        int g = 12;
        while (p != null && g-- > 0)
        {
            string pn = p.name;
            if (pn == null) pn = "";
            string pl = pn.ToLower();
            if (pl == "ui" || pl.IndexOf("selector") >= 0) return true;
            p = p.parent;
        }
        return false;
    }

    private void ForceVisibleHierarchy(Transform root)
    {
        if (root == null) return;

        // Force layer recursively (limited depth to keep safe)
        ForceLayerRec(root, forceVisibleLayer, 10);

        // Enable all renderers
        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] != null) rs[i].enabled = true;
        }

        // Play particles
        ParticleSystem[] ps = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            ParticleSystem p = ps[i];
            if (p == null) continue;
            var em = p.emission;
            em.enabled = true;
            if (!p.isPlaying) p.Play(true);
        }
    }

    private void ForceLayerRec(Transform root, int layer, int maxDepth)
    {
        if (root == null || maxDepth < 0) return;
        root.gameObject.layer = layer;
        if (maxDepth == 0) return;
        int c = root.childCount;
        for (int i = 0; i < c; i++)
        {
            ForceLayerRec(root.GetChild(i), layer, maxDepth - 1);
        }
    }


    private void EnableAllRenderers(GameObject go)
    {
        if (go == null) return;
        _tmpScanVisited = 0;
        EnableAllRenderersRec(go.transform, 8, 512);
    }

    private string NormalizeId(string s)
    {
        if (s == null) return "";
        string t = s.Trim();
        t = t.Replace(" ", "");
        t = t.Replace("_", "");
        t = t.Replace("-", "");
        return t.ToUpper();
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

    // =====================================================
    // Udon-safe helpers (NO tags, NO Resources.* (Editor-only), NO includeInactive overloads)
    // =====================================================

    private bool IsLikelyUIRoot(Transform tr)
    {
        if (tr == null) return false;

        // UI objects usually have RectTransform/Canvas
        if (tr.GetComponent<RectTransform>() != null) return true;
        if (tr.GetComponent<Canvas>() != null) return true;

        string n = tr.name;
        if (string.IsNullOrEmpty(n)) return false;
        n = n.ToUpper();
        if (n.Contains("CANVAS")) return true;
        if (n.Contains("UI")) return true;
        if (n.Contains("BUTTON")) return true;
        if (n.Contains("PANEL")) return true;

        return false;
    }

    private Transform FindByNameAnywhere(Transform context, string exactName)
    {
        if (string.IsNullOrEmpty(exactName)) return null;

        // Fast path (scene lookup)
        GameObject go = GameObject.Find(exactName);
        if (go != null) return go.transform;

        // Fallback: search from scene root
        Transform root = (context != null) ? context.root : null;
        if (root == null) return null;

        _tmpScanVisited = 0;
        return FindByExactNameRec(root, exactName, 10, 2048);
    }

    private Transform FindByExactNameRec(Transform tr, string exactName, int depthLeft, int maxNodes)
    {
        if (tr == null) return null;
        if (_tmpScanVisited >= maxNodes) return null;
        _tmpScanVisited++;

        if (tr.name == exactName) return tr;

        if (depthLeft <= 0) return null;

        int c = tr.childCount;
        for (int i = 0; i < c; i++)
        {
            Transform ch = tr.GetChild(i);
            Transform f = FindByExactNameRec(ch, exactName, depthLeft - 1, maxNodes);
            if (f != null) return f;
        }
        return null;
    }

    private Transform FindByNameContainsAnywhere1(Transform context, string tokenUpper)
    {
        if (string.IsNullOrEmpty(tokenUpper)) return null;
        Transform root = (context != null) ? context.root : null;
        if (root == null) return null;

        _tmpScanVisited = 0;
        return FindByContainsRec(root, tokenUpper, null, 10, 2048);
    }

    private Transform FindByNameContainsAnywhere2(Transform context, string tokenUpper1, string tokenUpper2)
    {
        if (string.IsNullOrEmpty(tokenUpper1) && string.IsNullOrEmpty(tokenUpper2)) return null;
        Transform root = (context != null) ? context.root : null;
        if (root == null) return null;

        _tmpScanVisited = 0;
        return FindByContainsRec(root, tokenUpper1, tokenUpper2, 10, 2048);
    }

    private Transform FindByContainsRec(Transform tr, string tokenUpper1, string tokenUpper2, int depthLeft, int maxNodes)
    {
        if (tr == null) return null;
        if (_tmpScanVisited >= maxNodes) return null;
        _tmpScanVisited++;

        string n = tr.name;
        if (!string.IsNullOrEmpty(n))
        {
            string up = n.ToUpper();
            bool ok = true;

            if (!string.IsNullOrEmpty(tokenUpper1) && !up.Contains(tokenUpper1)) ok = false;
            if (!string.IsNullOrEmpty(tokenUpper2) && !up.Contains(tokenUpper2)) ok = false;

            if (ok) return tr;
        }

        if (depthLeft <= 0) return null;

        int c = tr.childCount;
        for (int i = 0; i < c; i++)
        {
            Transform ch = tr.GetChild(i);
            Transform f = FindByContainsRec(ch, tokenUpper1, tokenUpper2, depthLeft - 1, maxNodes);
            if (f != null) return f;
        }
        return null;
    }

    private Transform FindBestPropsRootByRenderers(Transform sceneRoot)
    {
        if (sceneRoot == null) return null;

        Transform best = null;
        int bestCount = 0;

        // Scan limited set: root children + grandchildren as candidates
        int c0 = sceneRoot.childCount;
        for (int i = 0; i < c0; i++)
        {
            Transform ch0 = sceneRoot.GetChild(i);
            if (ch0 == null) continue;

            ConsiderPropsCandidate(ch0, ref best, ref bestCount);

            int c1 = ch0.childCount;
            for (int j = 0; j < c1; j++)
            {
                Transform ch1 = ch0.GetChild(j);
                if (ch1 == null) continue;
                ConsiderPropsCandidate(ch1, ref best, ref bestCount);
            }
        }

        return best;
    }

    private void ConsiderPropsCandidate(Transform tr, ref Transform best, ref int bestCount)
    {
        if (tr == null) return;
        if (IsLikelyUIRoot(tr)) return;

        string n = tr.name;
        if (string.IsNullOrEmpty(n)) return;
        string up = n.ToUpper();

        // Only consider plausible roots by name
        bool plausible = up.Contains("PROP") || up.Contains("TOOL") || up.Contains("EQUIP") || up.Contains("MODEL") || up.Contains("VR");
        if (!plausible) return;

        int count = CountRenderersUnder(tr, 5, 1024);
        if (count > bestCount)
        {
            bestCount = count;
            best = tr;
        }
    }

    private int CountRenderersUnder(Transform tr, int maxDepth, int maxNodes)
    {
        if (tr == null) return 0;
        _tmpScanVisited = 0;
        return CountRenderersUnderRec(tr, maxDepth, maxNodes);
    }

    private int CountRenderersUnderRec(Transform tr, int depthLeft, int maxNodes)
    {
        if (tr == null) return 0;
        if (_tmpScanVisited >= maxNodes) return 0;
        _tmpScanVisited++;

        int count = (tr.GetComponent<Renderer>() != null) ? 1 : 0;
        if (depthLeft <= 0) return count;

        int c = tr.childCount;
        for (int i = 0; i < c; i++)
        {
            count += CountRenderersUnderRec(tr.GetChild(i), depthLeft - 1, maxNodes);
        }
        return count;
    }

    private void EnableAllRenderersRec(Transform tr, int depthLeft, int maxNodes)
    {
        if (tr == null) return;
        if (_tmpScanVisited >= maxNodes) return;
        _tmpScanVisited++;

        Renderer r = tr.GetComponent<Renderer>();
        if (r != null) r.enabled = true;

        if (depthLeft <= 0) return;

        int c = tr.childCount;
        for (int i = 0; i < c; i++)
        {
            EnableAllRenderersRec(tr.GetChild(i), depthLeft - 1, maxNodes);
        }
    }

    private Transform GetTopChildUnderRoot(Transform leaf, Transform root)
    {
        if (leaf == null || root == null) return null;
        Transform cur = leaf;
        while (cur != null && cur.parent != null && cur.parent != root)
        {
            cur = cur.parent;
        }
        if (cur != null && cur.parent == root) return cur;
        return (leaf.parent == root) ? leaf : null;
    }

    private Transform FindBestToolTransformUnderRoot(Transform root, string normToolId)
    {
        if (root == null) return null;
        if (string.IsNullOrEmpty(normToolId)) return null;

        Transform best = null;
        int bestScore = 9999;
        int bestLenDiff = 9999;

        // 1) Check direct children (recommended structure)
        int c = root.childCount;
        for (int i = 0; i < c; i++)
        {
            Transform ch = root.GetChild(i);
            if (ch == null) continue;

            // Ignore obvious UI roots
            if (IsLikelyUIRoot(ch)) continue;

            string cn = NormalizeId(ch.name);
            int score = ScoreNameMatch(cn, normToolId);
            if (score < 9999)
            {
                int lenDiff = AbsInt(cn.Length - normToolId.Length);
                if (score < bestScore || (score == bestScore && lenDiff < bestLenDiff))
                {
                    bestScore = score;
                    bestLenDiff = lenDiff;
                    best = ch;
                }
            }
        }

        if (best != null) return best;

        // 2) Fallback: limited depth search (in case tools are nested)
        _tmpScanVisited = 0;
        return FindBestToolRec(root, normToolId, 4, 2048);
    }

    private Transform FindBestToolRec(Transform tr, string normToolId, int depthLeft, int maxNodes)
    {
        if (tr == null) return null;
        if (_tmpScanVisited >= maxNodes) return null;
        _tmpScanVisited++;

        if (!IsLikelyUIRoot(tr))
        {
            string cn = NormalizeId(tr.name);
            if (ScoreNameMatch(cn, normToolId) < 9999 && HasAnyRenderer(tr))
                return tr;
        }

        if (depthLeft <= 0) return null;

        int c = tr.childCount;
        for (int i = 0; i < c; i++)
        {
            Transform f = FindBestToolRec(tr.GetChild(i), normToolId, depthLeft - 1, maxNodes);
            if (f != null) return f;
        }
        return null;
    }

    private int ScoreNameMatch(string normCandidate, string normTarget)
    {
        if (string.IsNullOrEmpty(normCandidate) || string.IsNullOrEmpty(normTarget)) return 9999;
        if (normCandidate == normTarget) return 0;
        if (normCandidate.StartsWith(normTarget)) return 1;
        if (normCandidate.Contains(normTarget)) return 2;
        if (normTarget.Contains(normCandidate)) return 3;
        return 9999;
    }

    private int AbsInt(int v)
    {
        return (v < 0) ? -v : v;
    }

}