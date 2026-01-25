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

    [Tooltip("(Optional) Prefab-based runtime spawner. If enabled, tools are instantiated and effects are attached as children.")]
    public ChemRuntimeToolSpawner runtimeToolSpawner;

    [Header("Element Effect Runtime")]
    [Tooltip("If true, a runtime clone of sampleVisual is created and parented under the selected tool. This avoids moving the template object and guarantees the effect becomes a child of the tool.")]
    public bool cloneSampleVisualIntoTool = true;

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

    [Tooltip("器具内エフェクトを再生するまでの遅延秒数（親子付け/有効化の反映待ち）。0で即時。")]
    public float elementEffectPlayDelaySeconds = 0.02f;




    [Header("Effect Fit (gravity & bounds)")]
    [Tooltip("trueの場合、液体/ガスなどの見た目を重力(Vector3.up)に対して水平に保ちます（器具を傾けても液面が水平っぽく見える）。")]
    public bool keepEffectLevelToGravity = true;

    [Tooltip("trueの場合、毎フレーム器具のBoundsから『中』の位置へ追従させます（スポーン直後にズレる問題の最終手段）。")]
    public bool continuouslyFitEffectToTool = true;

    [Range(0.05f, 0.95f)]
    [Tooltip("器具Boundsの下端=0、上端=1 としたとき、エフェクト中心を置く高さ。0.25〜0.45が無難。")]
    public float effectFillHeight01 = 0.33f;

    [Tooltip("器具Bounds基準のワールドオフセット（微調整）。")]
    public Vector3 effectWorldOffset = Vector3.zero;

    [Tooltip("器具Boundsサイズを基準にしたスケール係数（x,zを少し小さめにすると『内側』に収まりやすい）。")]
    public Vector3 effectBoundsScale = new Vector3(0.85f, 0.55f, 0.85f);

    [Header("Auto BEAKER on element select")]
    [Tooltip("元素ボタン押下時、器具未選択なら自動で BEAKER を選択して表示します。")]
    public bool autoSpawnBeakerOnElement = true;

    [Tooltip("自動選択する器具ID（toolModelsRoot配下の名前に部分一致させます）。例: BEAKER")]
    public string autoBeakerToolId = "CONICAL_FLASK";

    [Tooltip("自動選択したBEAKERを containerTransform(VR_StartZone) の位置に移動します（親子付けは変えません）。")]
    public bool autoPlaceBeakerAtContainer = true;

    [Tooltip("BEAKERを containerTransform に置く際のワールドオフセット。")]
    public Vector3 autoBeakerWorldOffset = new Vector3(0f, 0.02f, 0f);

    [Header("Force visibility (debug-safe)")]
    [Tooltip("選択時に、BEAKER/SampleVisual の Renderer/Particle/Layer を強制的に可視化します。")]
    public bool forceVisibleOnSelect = true;

    [Tooltip("強制的に設定するLayer。0=Default。")]
    public int forceVisibleLayer = 0;



[Header("Runtime Spawn (Multi-instance)")]
[Tooltip("If true, every button press spawns a NEW instance (tools/elements do NOT replace previous ones).")]
public bool spawnNewInstancePerPress = true;

[Tooltip("Container tool used when an element button is pressed (ex: CONICAL_FLASK).")]
public string elementContainerToolId = "CONICAL_FLASK";

[Tooltip("Optional parent for spawned runtime objects (keeps hierarchy tidy). If null, uses ExperimentTable when found, otherwise world root.")]
public Transform runtimeSpawnParent;

[Tooltip("Optional explicit ExperimentTable root. If null, we auto-find a GameObject named 'ExperimentTable'.")]
public Transform experimentTableRoot;

[Tooltip("Random spawn radius (meters) around the spawn center.")]
public float spawnRadiusMeters = 0.85f;

[Tooltip("Minimum distance from the previous spawn (meters).")]
public float spawnMinSeparation = 0.25f;

[Tooltip("World Y offset added to spawn position.")]
public float spawnYOffset = 0.02f;

[Tooltip("Max retries to avoid spawning at the same spot.")]
public int spawnRetryCount = 16;

[Header("Per-Tool Reaction VFX (Clone)")]
[Tooltip("Template VFX under ExperimentTable/Effects/ReactionVFX. If null, auto-resolve at runtime.")]
public GameObject reactionVfxTemplate;

[Tooltip("Spawned VFX is parented under this anchor name inside the tool (fallback: tool root).")]
public string reactionVfxAnchorName = "ReactionVFXAnchor";

[Header("Preview Materials (Udon-safe)")]
[Tooltip("Optional explicit GlassMaster material. If null, auto-detect from templates.")]
public Material glassMasterMaterial;

[Tooltip("Optional explicit WireframeFX material. If null, auto-detect from templates.")]
public Material wireframeFxMaterial;

[Header("Particle Material (Udon-safe)")]
[Tooltip("Optional explicit particle material (must already exist in the project). Assign a ChemLab particle material here if your ParticleSystemRenderer has no material and particles are invisible. (UdonSharp cannot call Shader.Find at runtime.)")]
public Material particleFallbackMaterial;

// Runtime material instances (avoid modifying shared assets)
private Material _runtimeElementGlassMaterial;
private Material _runtimeFallbackVisibleGlassMaterial;

// Cached scene template for SampleVisual (important: prefab ElementVisual has no backing UdonBehaviour, so it stays invisible)
private GameObject _cachedSampleVisualTemplateGo;
private bool _cachedSampleVisualTemplateSearched;


private Vector3 _lastSpawnPos;
private int _runtimeSpawnSerial;

    // Reusable property block for particle clipping (keep particles visually inside glassware)
    private MaterialPropertyBlock _particleMpb;

    // Fallback particle material (fix: some prefabs ship with ParticleSystemRenderer.material = None)
    private Material _particleMasterMaterial;

    private Transform _activeToolTr;
    private Transform _activeToolTopTr;
    private Transform _activeAnchorTr;
    private string _lastToolApplied = "";
    private int _tmpScanVisited = 0;

    private GameObject _pendingEffectGo;
    private bool _pendingEffectRestart;


    // =====================================================
    // Runtime spawn tracking (for reliable Reset)
    // =====================================================
    private GameObject[] _runtimeSpawned = new GameObject[128];
    private int _runtimeSpawnedCount = 0;

    private void RegisterRuntimeSpawn(GameObject go)
    {
        if (go == null) return;
        if (_runtimeSpawnedCount >= _runtimeSpawned.Length) return;
        _runtimeSpawned[_runtimeSpawnedCount++] = go;
    }

    private void DestroyAllRuntimeSpawns()
    {
        for (int i = 0; i < _runtimeSpawnedCount; i++)
        {
            GameObject g = _runtimeSpawned[i];
            if (g != null) Destroy(g);
            _runtimeSpawned[i] = null;
        }
        _runtimeSpawnedCount = 0;
    }

    private void AutoWireSceneRefs()
    {
        // Tool spawner
        if (runtimeToolSpawner == null)
        {
            runtimeToolSpawner = GetComponent<ChemRuntimeToolSpawner>();
            if (runtimeToolSpawner == null)
            {
                GameObject g = GameObject.Find("ChemRuntimeToolSpawner");
                if (g != null) runtimeToolSpawner = g.GetComponent<ChemRuntimeToolSpawner>();
            }
        }

        // Databases
        if (elementDb == null)
        {
            GameObject g = GameObject.Find("ChemElementDatabase");
            if (g != null) elementDb = g.GetComponent<ChemElementDatabase>();
        }
        if (reactionDb == null)
        {
            GameObject g = GameObject.Find("ChemicalReactionDatabase");
            if (g != null) reactionDb = g.GetComponent<ChemicalReactionDatabase>();
        }

        // Experiment table root for spawn positioning
        if (experimentTableRoot == null)
        {
            GameObject g = GameObject.Find("ExperimentTable");
            if (g != null) experimentTableRoot = g.transform;
        }

        // Container anchor for previews (fallback)
        if (containerTransform == null)
        {
            Transform z = null;
            GameObject gz = GameObject.Find("VR_StartZone");
            if (gz != null) z = gz.transform;
            if (z == null)
            {
                GameObject gz2 = GameObject.Find("StartZone");
                if (gz2 != null) z = gz2.transform;
            }
            if (z == null) z = experimentTableRoot;
            containerTransform = (z != null) ? z : transform;
        }

        if (elementEffectAnchorFallback == null) elementEffectAnchorFallback = containerTransform;
    }

    // ToolMotionOpsEstimator helper
    public Transform GetActiveToolTransform()
    {
        // Prefer the actually spawned/placed tool top for motion estimators (tilt/shake/pour).
        if (_placedToolTopTr != null) return _placedToolTopTr;
        if (_activeToolTopTr != null) return _activeToolTopTr;
        if (_activeToolTr != null) return _activeToolTr;
        return containerTransform != null ? containerTransform : transform;
    }

    void Start()
    {
        AutoWireSceneRefs();
        EnsurePreviewRefs();
    }


    // --- Tool placement (non-destructive) ---
    // We never re-parent tools; we only move the selected tool's TOP transform under toolModelsRoot
    // to the containerTransform (VR_StartZone) and restore it when selection changes / reset.
    private Transform _placedToolTopTr;
    private Vector3 _placedToolOrigPos;
    private Quaternion _placedToolOrigRot;
    private Transform _placedToolOrigParent;
    private int _placedToolOrigSiblingIndex;
    private bool _hasPlacedToolOrig = false;

    // --- Runtime element visual instance (guarantee it becomes a child of the tool) ---
    private ChemVisualController _runtimeSampleVisual;
    private Transform _runtimeSampleVisualTr;
    private GameObject _runtimeSampleVisualGo;
    private bool _templateSampleHidden = false;
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

    private void Start_Duplicate_DO_NOT_USE()
    {
        if (containerTransform == null) containerTransform = transform;
        AutoWireSceneRefs();

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

    // -------------------------------------------------
    // Auto-wiring (no inspector required)
    // -------------------------------------------------
    private void AutoWireSceneRefs_Duplicate_DO_NOT_USE()
    {
        // containerTransform: prefer VR_StartZone (ExperimentTable上のZone)
        if (containerTransform == null || containerTransform == transform)
        {
            GameObject zoneGo = GameObject.Find("VR_StartZone");
            if (zoneGo != null) containerTransform = zoneGo.transform;
        }

        // toolModelsRoot: prefer VR_Props (3D props root)
        if (toolModelsRoot == null)
        {
            GameObject propsGo = GameObject.Find("VR_Props");
            if (propsGo != null) toolModelsRoot = propsGo.transform;
        }

        // fallback anchor
        if (elementEffectAnchorFallback == null && containerTransform != null)
            elementEffectAnchorFallback = containerTransform;

        // UdonSharpでは user-defined type に対する typeof() が使えないため、ジェネリック版を使用
        if (runtimeToolSpawner == null)
            runtimeToolSpawner = GetComponent<ChemRuntimeToolSpawner>();

        // If environment ref is missing but present in scene, try to find it
        if (environment == null)
        {
            GameObject envGo = GameObject.Find("ChemEnvironmentManager");
            if (envGo != null) environment = envGo.GetComponent<ChemEnvironmentManager>();
        }


        // Auto-wire SampleVisual (scene template with backing UdonBehaviour)
        // IMPORTANT: Prefab 'ElementVisual' has all states disabled and often has no backing UdonBehaviour,
        // so runtime-instantiated prefabs can remain invisible. We prefer the scene object named 'SampleVisual'.
        if (sampleVisual == null || sampleVisual.gameObject == null || sampleVisual.gameObject.name != "SampleVisual")
        {
            GameObject svGo = GameObject.Find("SampleVisual");
            if (svGo != null)
            {
                ChemVisualController sv = svGo.GetComponent<ChemVisualController>();
                if (sv != null) sampleVisual = sv;
            }
        }

        // Auto-wire ExperimentTable root for runtime spawning
        if (experimentTableRoot == null)
        {
            GameObject tableGo = GameObject.Find("ExperimentTable");
            if (tableGo != null) experimentTableRoot = tableGo.transform;
        }
        if (runtimeSpawnParent == null && experimentTableRoot != null) runtimeSpawnParent = experimentTableRoot;
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
        return _syncedOperatorPlayerId > 0;
    }

    private bool EnsureCanControl()
    {
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp == null) return true;

        // operator未選択なら、押した人を操作者にする（自動取得）
        if (_syncedOperatorPlayerId <= 0)
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
        AutoWireSceneRefs();
        EnsurePreviewRefs();

        

if (!EnsureCanControl()) return;

_localInput = (symbolOrFormula == null) ? "" : symbolOrFormula.Trim();

// Normalize element identifier:
// UI buttons sometimes pass Japanese/English element names instead of symbols.
// For visuals & DB lookup we must store the SYMBOL (e.g. "Na").
// If it cannot be resolved, keep the original token.
if (elementDb != null)
{
    string resolved = elementDb.ResolveSymbol(_localInput);
    if (!string.IsNullOrEmpty(resolved)) _localInput = resolved;
}

_syncedInput = _localInput;

_syncedVersion++;
RequestSerialization();

AppendHistory("SelectElement: " + _localInput);

// Always spawn a NEW container instance every press.
// NOTE: Unity serialization sets newly-added bool fields on existing components to
// their type default (false). We cannot rely on inspector state here.
SpawnElementContainerInstance(_localInput);

// Keep UI/state updates for experiment controls
if (sampleVisual != null)
    sampleVisual.NotifyElementSelected(_localInput);

ApplyVisualFromState(true);
WriteUI();
    }

    public void SelectEquipment(string toolId)
    {
        AutoWireSceneRefs();
        EnsurePreviewRefs();

        

if (!EnsureCanControl()) return;

_localTool = (toolId == null) ? "" : toolId.Trim();
_syncedTool = _localTool;

_syncedVersion++;
RequestSerialization();

AppendHistory("SelectEquipment: " + _localTool);
WriteUI();

// Always spawn a NEW tool instance every press.
SpawnToolInstance(_localTool, true);
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
        AutoWireSceneRefs();
        EnsurePreviewRefs();

        if (!EnsureCanControl()) return;

        // Destroy any runtime-spawned tools/effects (submission-safe reset)
        DestroyAllRuntimeSpawns();

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
        RestorePlacedToolLocal(false);
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

        ChemVisualController vis = GetActiveVisualController();
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

// エフェクトを器具の『中』へ追従（重力に水平）
        TickEffectFitLocal();

        // フェーズ遷移を検出してローカル演出へ通知（主にcomplete）
        if (_syncedPhase != _lastAppliedPhase)
        {
            if (vis != null && _syncedPhase == 2)
            {
                vis.NotifyReactionComplete(_syncedProductFormula, _syncedReactionTag);
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

    
    private ChemVisualController GetActiveVisualController()
    {
        // When cloneSampleVisualIntoTool is enabled, the visible visual is the runtime clone.
        if (_runtimeSampleVisual != null) return _runtimeSampleVisual;
        return sampleVisual;
    }

private void ApplyVisualContinuous()
    {
        ChemVisualController vis = GetActiveVisualController();
        if (vis == null || elementDb == null) return;

        string sym = GetDisplayFormula();
        vis.ApplyElementBySymbol(elementDb, sym, _visualTempC);
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

            // Force the AI visual preset to match the synced 'truth' (predictor result)
            ai.predictedProductFormula = _syncedProductFormula;
            ai.predictedReactionTag = _syncedReactionTag;
            ai.EvaluateAtProgress(_syncedProgress01);


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
        ChemVisualController vis = GetActiveVisualController();
        if (vis == null || elementDb == null) return;

        string sym = GetDisplayFormula();
        vis.ApplyElementBySymbol(elementDb, sym, _visualTempC);

        // 完了フェーズ通知（ローカル演出）
        if (_syncedPhase != _lastAppliedPhase)
        {
            if (vis != null && _syncedPhase == 2)
            {
                vis.NotifyReactionComplete(_syncedProductFormula, _syncedReactionTag);
            }
            _lastAppliedPhase = _syncedPhase;
        }
    }

    // =====================================================
    // 3D Preview helpers (UI-safe)
    // =====================================================
    private void EnsurePreviewRefs()
    {
        // Prefer placing previews in the ExperimentTable zone (VR_StartZone) if it exists.
        if (containerTransform == null)
        {
            Transform z = FindByNameAnywhere(transform.root, "VR_StartZone");
            if (z == null) z = FindByNameAnywhere(transform.root, "StartZone");
            if (z == null) z = FindByNameAnywhere(transform.root, "Zone");
            containerTransform = (z != null) ? z : transform;
        }
        if (elementEffectAnchorFallback == null) elementEffectAnchorFallback = containerTransform;

        // Tool templates root: prefer toolModelsRoot (Hierarchyの「Tool」) を Spawner に渡す
        if (runtimeToolSpawner != null && runtimeToolSpawner.toolTemplatesRoot == null)
        {
            if (toolModelsRoot != null) runtimeToolSpawner.toolTemplatesRoot = toolModelsRoot;
            else
            {
                Transform t = FindByNameAnywhere(transform.root, "Tool");
                if (t != null) runtimeToolSpawner.toolTemplatesRoot = t;
            }
        }


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

        // Determine tool id
        string toolId = string.IsNullOrEmpty(_syncedTool) ? _localTool : _syncedTool;
        if (toolId == null) toolId = "";
        string norm = NormalizeId(toolId);

        if (!force && norm == _lastToolApplied) return;
        _lastToolApplied = norm;

        // Clear current clone if no tool
        if (string.IsNullOrEmpty(norm))
        {
            ClearActiveToolClone();
            return;
        }

        // Ensure spawner has a correct template root (prefer Hierarchy "Tool")
        Transform templatesRoot = FindToolTemplatesRoot();
        if (runtimeToolSpawner != null && templatesRoot != null)
        {
            runtimeToolSpawner.toolTemplatesRoot = templatesRoot;
            // Do NOT hide / deactivate templates during presentation
            runtimeToolSpawner.hideTemplateRenderers = false;
            runtimeToolSpawner.deactivateTemplateObject = false;
        }

        // Always spawn a clone (never "bring" an existing object)
        Transform spawned = null;
        if (runtimeToolSpawner != null && containerTransform != null)
        {
            spawned = runtimeToolSpawner.SpawnTool(norm, containerTransform, autoBeakerWorldOffset, true);
        }
        else
        {
            // Direct fallback clone if spawner is missing
            spawned = SpawnToolCloneDirect(norm, containerTransform, autoBeakerWorldOffset);
        }

        if (spawned == null)
        {
            // keep nothing rather than moving an existing tool
            ClearActiveToolClone();
            return;
        }

        _activeToolTr = spawned;
        _activeToolTopTr = spawned;
        LiftToolAboveTable(spawned);
    }

    
    private void LiftToolAboveTable(Transform toolTr)
    {
        if (toolTr == null) return;
        if (experimentTableRoot == null) return;

        // Find the largest collider under the experiment table (likely the tabletop)
        Collider[] tableCols = experimentTableRoot.GetComponentsInChildren<Collider>(true);
        if (tableCols == null || tableCols.Length == 0) return;

        Collider tableCol = null;
        float bestVol = -1f;
        for (int i = 0; i < tableCols.Length; i++)
        {
            Collider c = tableCols[i];
            if (c == null || !c.enabled) continue;
            Bounds b = c.bounds;
            float v = b.size.x * b.size.y * b.size.z;
            if (v > bestVol)
            {
                bestVol = v;
                tableCol = c;
            }
        }
        if (tableCol == null) return;

        // Determine tool bounds
        Bounds tb;
        if (!TryGetRenderableBounds(toolTr, out tb))
        {
            Collider[] toolCols = toolTr.GetComponentsInChildren<Collider>(true);
            if (toolCols == null || toolCols.Length == 0) return;
            tb = toolCols[0].bounds;
        }

        float tableTopY = tableCol.bounds.max.y;
        float toolMinY = tb.min.y;

        if (toolMinY < tableTopY + 0.005f)
        {
            float dy = (tableTopY + 0.01f) - toolMinY;
            toolTr.position += new Vector3(0f, dy, 0f);
        }
    }

// =====================================================
    // Tool Clone Helpers (Presentation-stable)
    // =====================================================

    private Transform _cachedToolTemplatesRoot;

    private void ClearActiveToolClone()
    {
        if (_activeToolTr != null)
        {
            Object.Destroy(_activeToolTr.gameObject);
        }
        _activeToolTr = null;
        _activeToolTopTr = null;
    }

    private Transform FindToolTemplatesRoot()
    {
        if (_cachedToolTemplatesRoot != null) return _cachedToolTemplatesRoot;

        // 1) Scene-level names (but NEVER pick UI Canvas objects named "Tool")
        GameObject g = GameObject.Find("Tool");
        if (g != null && IsLikelyUIRoot(g.transform)) g = null;

        if (g == null)
        {
            g = GameObject.Find("Tools");
            if (g != null && IsLikelyUIRoot(g.transform)) g = null;
        }

        if (g == null)
        {
            g = GameObject.Find("ToolTemplates");
            if (g != null && IsLikelyUIRoot(g.transform)) g = null;
        }

        if (g == null)
        {
            g = GameObject.Find("Tool Templates");
            if (g != null && IsLikelyUIRoot(g.transform)) g = null;
        }

        if (g != null)
        {
            _cachedToolTemplatesRoot = g.transform;
            return _cachedToolTemplatesRoot;
        }

        // 2) Under ExperimentTable (common layout)
        Transform et = ResolveExperimentTableRoot();
        if (et != null)
        {
            Transform[] all = et.GetComponentsInChildren<Transform>(true);
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t == null) continue;
                    if (t.name != "Tool" && t.name != "Tools" && t.name != "ToolTemplates" && t.name != "Tool Templates") continue;
                    if (IsLikelyUIRoot(t)) continue;
                    _cachedToolTemplatesRoot = t;
                    return _cachedToolTemplatesRoot;
                }
            }
        }

        // 3) last resort: use toolModelsRoot if present (3D side)
        if (toolModelsRoot != null && !IsLikelyUIRoot(toolModelsRoot))
        {
            _cachedToolTemplatesRoot = toolModelsRoot;
            return _cachedToolTemplatesRoot;
        }

        return null;
    }

    private Transform SpawnToolCloneDirect(string toolIdNorm, Transform parent, Vector3 worldOffset)
    {
        if (parent == null) return null;

        Transform root = FindToolTemplatesRoot();
        if (root == null) return null;

        Transform template = FindTemplateInDescendants(root, toolIdNorm);
        if (template == null) return null;

        GameObject go = VRCInstantiate(template.gameObject);
        if (go == null) return null;

        go.transform.SetParent(parent, true);
        go.transform.position = parent.position + worldOffset;
        go.transform.rotation = parent.rotation;
        if (!go.activeSelf) go.SetActive(true);

    // Force all descendants active (templates sometimes keep render children disabled).
    ForceActiveDescendants(go.transform, 4096);

    if (forceVisibleOnSelect) ForceVisibleHierarchy(go.transform);
        return go.transform;
    }

    private Transform FindTemplateInDescendants(Transform root, string toolIdNorm)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        if (all == null) return null;

        int n = all.Length;
        for (int i = 0; i < n; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t == root) continue;

            string baseName = StripUnitySuffixLocal(t.name);
            string normName = NormalizeId(baseName);

            if (normName == toolIdNorm) return t;
            if (NormalizeId(baseName + "_PICKUP") == toolIdNorm) return t;

            if (normName.EndsWith("_PICKUP"))
            {
                string noPickup = normName.Replace("_PICKUP", "");
                if (NormalizeId(noPickup) == toolIdNorm) return t;
            }
        }
        return null;
    }

    private string StripUnitySuffixLocal(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim();

        int p = s.LastIndexOf('(');
        if (p > 0 && s.EndsWith(")"))
        {
            if (s[p - 1] == ' ')
            {
                s = s.Substring(0, p - 1).Trim();
            }
        }
        return s;
    }


    private void PlaceSelectedToolAtContainerLocal()
    {
        // Reuse existing option flag to avoid adding inspector work
        if (!autoPlaceBeakerAtContainer) 
        {
            // If placement is disabled, still restore if we previously moved something
            RestorePlacedToolLocal(false);
            return;
        }

        EnsurePreviewRefs();
        if (containerTransform == null)
        {
            RestorePlacedToolLocal(false);
            return;
        }

        // Determine current selected tool
        string toolId = string.IsNullOrEmpty(_syncedTool) ? _localTool : _syncedTool;
        if (toolId == null) toolId = "";
        string norm = NormalizeId(toolId);

        // If no tool selected, restore any moved tool
        if (string.IsNullOrEmpty(norm))
        {
            RestorePlacedToolLocal(false);
            return;
        }

        // Ensure we have active tool resolved
        if (_activeToolTr == null && toolModelsRoot != null)
        {
            _activeToolTr = FindBestToolTransformUnderRoot(toolModelsRoot, norm);
            _activeToolTopTr = GetTopChildUnderRoot(_activeToolTr, toolModelsRoot);
        }

        Transform top = (_activeToolTopTr != null) ? _activeToolTopTr : _activeToolTr;
        if (top == null) 
        {
            RestorePlacedToolLocal(false);
            return;
        }

        // If player is holding it, don't force-move (prevents breaking interactions)
        VRC_Pickup pickup = top.GetComponent<VRC_Pickup>();
        if (pickup != null && pickup.IsHeld) return;

        // If switching tools, restore previous
        if (_placedToolTopTr != null && _placedToolTopTr != top)
        {
            RestorePlacedToolLocal(false);
        }

        // Cache original transform once
        if (_placedToolTopTr == null)
        {
            _placedToolTopTr = top;
            _placedToolOrigParent = top.parent;
            _placedToolOrigSiblingIndex = top.GetSiblingIndex();
            _placedToolOrigPos = top.position;
            _placedToolOrigRot = top.rotation;
            _hasPlacedToolOrig = true;
        }

        // Place at container zone (VR_StartZone)
        if (top.parent != containerTransform) top.SetParent(containerTransform, true);
        top.position = containerTransform.position + autoBeakerWorldOffset;
        top.rotation = containerTransform.rotation;

        ActivateParents(top);
        if (!top.gameObject.activeSelf) top.gameObject.SetActive(true);
    }

    private void RestorePlacedToolLocal(bool deactivateIfHidden)
    {
        if (_placedToolTopTr == null || !_hasPlacedToolOrig) return;

        // If held, don't teleport it back; let the player drop it first
        VRC_Pickup pickup = _placedToolTopTr.GetComponent<VRC_Pickup>();
        if (pickup != null && pickup.IsHeld) return;

                if (_placedToolOrigParent != null) {
            _placedToolTopTr.SetParent(_placedToolOrigParent, true);
            _placedToolTopTr.SetSiblingIndex(_placedToolOrigSiblingIndex);
        }

_placedToolTopTr.position = _placedToolOrigPos;
        _placedToolTopTr.rotation = _placedToolOrigRot;

        if (deactivateIfHidden && _placedToolTopTr.gameObject.activeSelf)
            _placedToolTopTr.gameObject.SetActive(false);

        _placedToolTopTr = null;
        _placedToolOrigParent = null;
        _hasPlacedToolOrig = false;
    }


    private void PlaceElementEffectLocal(bool force)
    {
        if (!placeElementEffectInTool) return;

        EnsurePreviewRefs();
        if (sampleVisual == null) return;

        // Make sure we have an active tool resolved (needed for anchor placement)
        if (_activeToolTr == null)
        {
            ApplyToolPreviewLocal(true);
        }

        Transform toolTop = (_activeToolTopTr != null) ? _activeToolTopTr : _activeToolTr;

        // Resolve anchor inside the tool if possible
        Transform anchor = null;
        if (toolTop != null)
        {
            string normAnchor = NormalizeId(elementEffectAnchorName);
            if (!string.IsNullOrEmpty(normAnchor))
                anchor = FindChildByNormContains(toolTop, normAnchor, 8, 1024);

            if (anchor == null)
                anchor = FindChildByAnyName(toolTop, _commonAnchorNames, 8, 1024);
        }

        // If anchor still not found, we will attach to toolTop and compute a reasonable local position from bounds.
        Transform parentTr = (anchor != null) ? anchor : toolTop;

        if (parentTr == null)
        {
            // Final fallback: keep it at container
            _activeAnchorTr = ResolveAnchor();
            if (_activeAnchorTr == null) return;
            parentTr = _activeAnchorTr;
        }

        _activeAnchorTr = parentTr;

        // --- IMPORTANT ---
        // Instead of moving the template sampleVisual object around (which often remains stuck near UI),
        // clone it once and always parent the runtime clone under the selected tool.
        ChemVisualController vis = GetOrCreateRuntimeSampleVisual(parentTr);
        if (vis == null) return;

        GameObject svGo = vis.gameObject;
        if (!svGo.activeSelf) svGo.SetActive(true);

        Transform svT = vis.transform;
        if (svT.parent != parentTr)
            svT.SetParent(parentTr, false);

        // 初期配置：Boundsから『中』へ寄せ、スケールも器具サイズへ合わせる
        TickEffectFitLocal();

        // まだ器具が解決できない場合の最低限フォールバック
        if (!continuouslyFitEffectToTool)
        {
            svT.localPosition = elementEffectLocalOffset;
            svT.localRotation = Quaternion.identity;
            // Compensate parent scale so the effect stays visible even under scaled tools
            Vector3 p = (svT.parent != null) ? svT.parent.lossyScale : Vector3.one;
            Vector3 desiredWorld = elementEffectLocalScale;
            Vector3 ls = desiredWorld;
            if (p.x != 0f) ls.x = desiredWorld.x / p.x;
            if (p.y != 0f) ls.y = desiredWorld.y / p.y;
            if (p.z != 0f) ls.z = desiredWorld.z / p.z;
            svT.localScale = ls;
        }

        // The runtime clone might be configured before its Start() runs; force-init to be safe.
        vis.EnsureInitialized();

        // Apply visual state (color/state) from DB if possible
        if (elementDb != null)
            vis.ApplyElementBySymbol(elementDb, GetDisplayFormula(), _visualTempC);

        // Ensure shader + particle VFX are actually visible.
        EnableAllRenderers(svGo);
        PlayAllParticles(svGo, true);

        if (forceVisibleOnSelect)
        {
            ForceVisibleHierarchy(svGo.transform);
            if (_activeToolTr != null) ForceVisibleHierarchy(_activeToolTr);
        }
    }

    private ChemVisualController GetOrCreateRuntimeSampleVisual(Transform desiredParent)
    {
        // If cloning is disabled, fall back to using the template directly.
        if (!cloneSampleVisualIntoTool)
            return sampleVisual;

        if (sampleVisual == null) return null;

        // Hide the template so it doesn't remain floating somewhere else.
        if (!_templateSampleHidden)
        {
            // Udon does not support exceptions; keep it simple.
            sampleVisual.gameObject.SetActive(false);
            _templateSampleHidden = true;
        }

        // Create runtime instance once
        if (_runtimeSampleVisualGo == null)
        {
            GameObject inst = VRCInstantiate(sampleVisual.gameObject);
            if (inst == null) return null;

            inst.name = "SampleVisual_Runtime";
            _runtimeSampleVisualGo = inst;
            _runtimeSampleVisualTr = inst.transform;

            _runtimeSampleVisual = inst.GetComponent<ChemVisualController>();
            if (_runtimeSampleVisual == null)
                _runtimeSampleVisual = inst.GetComponentInChildren<ChemVisualController>(true);

            inst.SetActive(true);
        }

        // Always parent to the currently active tool/anchor
        if (_runtimeSampleVisualTr != null && desiredParent != null && _runtimeSampleVisualTr.parent != desiredParent)
            _runtimeSampleVisualTr.SetParent(desiredParent, false);

        return _runtimeSampleVisual;
    }

    private bool TryGetRenderableBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (root == null) return false;

        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs == null || rs.Length == 0) return false;

        bool has = false;
        int n = rs.Length;
        for (int i = 0; i < n; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            if (!r.enabled) continue;

            // Skip UI-ish renderers (very common cause of "effect goes to canvas area")
            Transform t = r.transform;
            if (t != null && IsLikelyUIRoot(t)) continue;

            if (!has)
            {
                bounds = r.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return has;
    }

        
    // =====================================================
    // Effect fit helpers (local-only)
    // =====================================================
    private void TickEffectFitLocal()
    {
        if (!continuouslyFitEffectToTool) return;

        // runtime clone が無いなら何もしない（テンプレ移動方式でもOK）
        ChemVisualController vis = null;
        if (cloneSampleVisualIntoTool)
        {
            vis = _runtimeSampleVisual;
            if (vis == null || _runtimeSampleVisualTr == null) return;
        }
        else
        {
            vis = sampleVisual;
            if (vis == null) return;
        }

        // アクティブ器具が無いなら何もしない
        if (_activeToolTopTr == null && _activeToolTr == null) return;

        Transform toolTop = (_activeToolTopTr != null) ? _activeToolTopTr : _activeToolTr;
        if (toolTop == null) return;

        Bounds b;
        if (!TryGetRenderableBounds(toolTop, out b)) return;

        Transform t = vis.transform;

        // 位置：Bounds中心XYを使って『中』へ
        float y = b.min.y + b.size.y * Mathf.Clamp01(effectFillHeight01);
        Vector3 wp = new Vector3(b.center.x, y, b.center.z) + effectWorldOffset;

        t.position = wp;

        // 回転：重力に対して水平（器具を傾けても液面が水平っぽく見える）
        if (keepEffectLevelToGravity)
        {
            // ワールドUp固定。前方はワールド前方で固定（液体表現ならこれで十分）
            t.rotation = Quaternion.identity;
        }

        // スケール：器具のサイズに合わせて収める（内側に入るように係数を掛ける）
        Vector3 size = b.size;
        Vector3 s = new Vector3(size.x * effectBoundsScale.x, size.y * effectBoundsScale.y, size.z * effectBoundsScale.z);
        // NOTE: b.size is in WORLD space. If the tool hierarchy is scaled (e.g. 0.01),
        // setting localScale directly will make the effect almost invisible.
        // We convert the desired WORLD scale into a localScale that compensates parent lossyScale.
        Vector3 desiredWorldScale = Vector3.Scale(elementEffectLocalScale, s);
        Vector3 parentLossy = (t.parent != null) ? t.parent.lossyScale : Vector3.one;
        Vector3 localScale = desiredWorldScale;
        if (parentLossy.x != 0f) localScale.x = desiredWorldScale.x / parentLossy.x;
        if (parentLossy.y != 0f) localScale.y = desiredWorldScale.y / parentLossy.y;
        if (parentLossy.z != 0f) localScale.z = desiredWorldScale.z / parentLossy.z;
        t.localScale = localScale;
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
                _activeToolTopTr = GetTopChildUnderRoot(_activeToolTr, toolModelsRoot);
            }
        }

        // Prefer tool's explicit anchor first
        if (_activeToolTr != null)
        {
            Transform a = FindChildByName(_activeToolTr, elementEffectAnchorName, 6);
            if (a != null) return a;

            // Case-insensitive / normalized search
            string normAnchor = NormalizeId(elementEffectAnchorName);
            a = FindChildByNormContains(_activeToolTr, normAnchor, 6, 512);
            if (a != null) return a;

            // Common container/contents anchors (robust fallback)
            a = FindChildByAnyName(_activeToolTr, _commonAnchorNames, 6, 512);
            if (a != null) return a;

            // Fallback to tool root
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

    // Common anchor name candidates inside tool prefabs (beaker/flask etc.)
    private string[] _commonAnchorNames = new string[]
    {
        "LIQUID", "LIQUIDSURFACE", "LIQUID_SURFACE", "LIQUIDCONTAINER", "LIQUID_CONTAINER",
        "CONTENTS", "CONTENT", "INSIDE", "INNER", "FILL", "FLUID", "WATER",
        "EFFECTANCHOR", "ANCHOR", "POUR", "SPOUT", "MOUTH"
    };

    private Transform FindChildByNormContains(Transform root, string normTarget, int maxDepth, int maxNodes)
    {
        if (root == null || string.IsNullOrEmpty(normTarget) || maxDepth < 0) return null;
        if (maxNodes <= 0) return null;

        int c = root.childCount;
        for (int i = 0; i < c; i++)
        {
            if (maxNodes-- <= 0) return null;

            Transform ch = root.GetChild(i);
            if (ch == null) continue;

            string nn = NormalizeId(ch.name);
            if (!string.IsNullOrEmpty(nn) && (nn == normTarget || nn.Contains(normTarget) || normTarget.Contains(nn)))
                return ch;
        }

        if (maxDepth == 0) return null;

        for (int i = 0; i < c; i++)
        {
            if (maxNodes-- <= 0) return null;
            Transform found = FindChildByNormContains(root.GetChild(i), normTarget, maxDepth - 1, maxNodes);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindChildByAnyName(Transform root, string[] candidates, int maxDepth, int maxNodes)
    {
        if (root == null || candidates == null || candidates.Length == 0 || maxDepth < 0) return null;
        if (maxNodes <= 0) return null;

        int c = root.childCount;
        for (int i = 0; i < c; i++)
        {
            if (maxNodes-- <= 0) return null;

            Transform ch = root.GetChild(i);
            if (ch == null) continue;

            string nn = NormalizeId(ch.name);
            if (string.IsNullOrEmpty(nn)) continue;

            for (int k = 0; k < candidates.Length; k++)
            {
                string cand = candidates[k];
                if (string.IsNullOrEmpty(cand)) continue;
                string nc = NormalizeId(cand);
                if (string.IsNullOrEmpty(nc)) continue;
                if (nn == nc || nn.Contains(nc) || nc.Contains(nn))
                    return ch;
            }
        }

        if (maxDepth == 0) return null;

        for (int i = 0; i < c; i++)
        {
            if (maxNodes-- <= 0) return null;
            Transform found = FindChildByAnyName(root.GetChild(i), candidates, maxDepth - 1, maxNodes);
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

        // Resolve tool transform and activate it (non-destructive)
        if (toolModelsRoot != null && !string.IsNullOrEmpty(autoBeakerToolId))
        {
            string norm = NormalizeId(autoBeakerToolId);
            _activeToolTr = FindBestToolTransformUnderRoot(toolModelsRoot, norm);
            _activeToolTopTr = GetTopChildUnderRoot(_activeToolTr, toolModelsRoot);

            Transform top = (_activeToolTopTr != null) ? _activeToolTopTr : _activeToolTr;
            if (top != null)
            {
                ActivateParents(top);
                if (!top.gameObject.activeSelf) top.gameObject.SetActive(true);

                // Place to container zone using the same path as normal selection
                PlaceSelectedToolAtContainerLocal();
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


    /// <summary>
    /// Force-enable all descendant GameObjects (Udon-safe). Useful when templates have children disabled,
    /// which would otherwise make renderers/particles never show.
    /// </summary>
    private void ForceActiveDescendants(Transform root, int maxNodes)
    {
        if (root == null) return;

        Transform[] trs = root.GetComponentsInChildren<Transform>(true);
        if (trs == null) return;

        int n = trs.Length;
        if (maxNodes > 0 && n > maxNodes) n = maxNodes;

        for (int i = 0; i < n; i++)
        {
            Transform t = trs[i];
            if (t == null) continue;
            GameObject g = t.gameObject;
            if (g == null) continue;
            if (!g.activeSelf) g.SetActive(true);
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

    /// <summary>
    /// Make sure all ParticleSystems under a root are emitting and playing.
    /// This is intentionally separate from ForceVisibleHierarchy so we can use it
    /// without touching layers.
    /// </summary>
    

    // -----------------------------------------
    // Delayed particle play (Udon-safe)
    // 親子付け・Active反映が1フレーム遅れるケースがあるため、遅延再生にする
    // -----------------------------------------
    private void ScheduleLatePlayEffect(GameObject effectRoot, bool restart)
    {
        if (effectRoot == null) return;
        _pendingEffectGo = effectRoot;
        _pendingEffectRestart = restart;

        // If delay is zero, still delay by 1 frame to ensure hierarchy is settled.
        if (elementEffectPlayDelaySeconds <= 0f)
        {
            SendCustomEventDelayedFrames(nameof(_LatePlayPendingEffect), 1);
        }
        else
        {
            SendCustomEventDelayedSeconds(nameof(_LatePlayPendingEffect), elementEffectPlayDelaySeconds);
        }
    }

    public void _LatePlayPendingEffect()
    {
        if (_pendingEffectGo == null) return;

        // Ensure hierarchy is active before playing
        ForceActiveDescendants(_pendingEffectGo.transform, 8192);
        EnableAllRenderers(_pendingEffectGo);
        PlayAllParticles(_pendingEffectGo, _pendingEffectRestart);
        if (forceVisibleOnSelect) ForceVisibleHierarchy(_pendingEffectGo.transform);

        // Clear to avoid re-playing unexpectedly
        _pendingEffectGo = null;
        _pendingEffectRestart = false;
    }

private void PlayAllParticles(GameObject rootGo, bool restart)
    {
        if (rootGo == null) return;

        // Some ParticleSystem prefabs (especially when cloned from inactive templates)
        // end up with ParticleSystemRenderer.sharedMaterial = null, making the effect invisible.
        // Resolve a reasonable master material and assign it to missing particle renderers.
        Material masterMat = GetParticleMasterMaterial();
        if (masterMat == null)
        {
            // Fallback: find any particle renderer under this instance that already has a material.
            ParticleSystemRenderer[] prs = rootGo.GetComponentsInChildren<ParticleSystemRenderer>(true);
            if (prs != null)
            {
                for (int i = 0; i < prs.Length; i++)
                {
                    ParticleSystemRenderer pr0 = prs[i];
                    if (pr0 == null) continue;
                    if (pr0.sharedMaterial != null)
                    {
                        masterMat = pr0.sharedMaterial;
                        break;
                    }
                }
            }
        }

        ParticleSystem[] ps = rootGo.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            ParticleSystem p = ps[i];
            if (p == null) continue;

            var em = p.emission;
            em.enabled = true;

            // Ensure its renderer is enabled as well
            ParticleSystemRenderer pr = p.GetComponent<ParticleSystemRenderer>();
            if (pr != null)
            {
                pr.enabled = true;

                // If the renderer has no material, the particles will not be visible.
                // Assign the resolved master material.
                if (pr.sharedMaterial == null && masterMat != null)
                {
                    pr.sharedMaterial = masterMat;
                }
            }

            if (restart)
            {
                // Clear + Play gives the most reliable result when the prefab was inactive at instantiation.
                p.Clear(true);
                p.Play(true);
            }
            else
            {
                if (!p.isPlaying) p.Play(true);
            }
        }
    }


    private void EnableAllRenderers(GameObject go)
    {
        if (go == null) return;
        _tmpScanVisited = 0;
        EnableAllRenderersRec(go.transform, 8, 512);

        // Also ensure particle systems are actually emitting.
        // (They often do not auto-play when cloned from an inactive template.)
        PlayAllParticles(go, false);
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



// =====================================================
// Multi-instance spawning (Udon-safe, no Shader.Find, no FindObjectsOfType, no try/catch)
// =====================================================

private Transform ResolveExperimentTableRoot()
{
    if (experimentTableRoot != null) return experimentTableRoot;
    GameObject g = GameObject.Find("ExperimentTable");
    if (g != null) experimentTableRoot = g.transform;
    return experimentTableRoot;
}

private Transform ResolveRuntimeSpawnParent()
{
    if (runtimeSpawnParent != null) return runtimeSpawnParent;
    Transform t = ResolveExperimentTableRoot();
    if (t != null) return t;
    return null;
}

private Vector3 GetRandomSpawnPosition()
{
    Transform centerTr = ResolveExperimentTableRoot();
    Vector3 center = (centerTr != null) ? centerTr.position : (containerTransform != null ? containerTransform.position : transform.position);

    Vector3 pos = center;
    for (int i = 0; i < spawnRetryCount; i++)
    {
        Vector2 r = Random.insideUnitCircle * spawnRadiusMeters;
        pos = new Vector3(center.x + r.x, center.y + 0.8f, center.z + r.y);

        // Raycast down to find a stable surface (table / floor)
        RaycastHit hit;
        if (Physics.Raycast(pos, Vector3.down, out hit, 3.0f))
        {
            pos.y = hit.point.y + Mathf.Max(0.02f, spawnYOffset);
        }
        else
        {
            // fallback: keep it slightly above center
            pos.y = center.y + 0.25f + spawnYOffset;
        }

        if ((_lastSpawnPos - pos).sqrMagnitude >= (spawnMinSeparation * spawnMinSeparation))
        {
            _lastSpawnPos = pos;
            return pos;
        }
    }

    // fallback (even if close)
    _lastSpawnPos = pos;
    return pos;
}

private string NormalizeIdRuntime(string s)
{
    if (string.IsNullOrEmpty(s)) return "";
    s = s.Trim().ToUpperInvariant();
    s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
    return s;
}



private GameObject GetToolPrefabByIdRuntime(string toolId)
{
    if (runtimeToolSpawner == null) return null;
    if (runtimeToolSpawner.toolPrefabs == null || runtimeToolSpawner.toolIds == null) return null;

    string want = NormalizeIdRuntime(toolId);
    int n = runtimeToolSpawner.toolIds.Length;
    if (runtimeToolSpawner.toolPrefabs.Length < n) n = runtimeToolSpawner.toolPrefabs.Length;

    for (int i = 0; i < n; i++)
    {
        string id = runtimeToolSpawner.toolIds[i];
        if (NormalizeIdRuntime(id) == want)
        {
            return runtimeToolSpawner.toolPrefabs[i];
        }
    }
    return null;
}

private void HideTemplateVisualIfInHierarchy(GameObject templateGo)
{
    if (templateGo == null) return;

    // Only mute if it is an actual scene object (prefab assets are not in the hierarchy).
    // IMPORTANT: do NOT deactivate it, because ResolveSampleVisualTemplateRuntime() may rely on GameObject.Find.
    // We just stop its particles + hide renderers so the scene template doesn't flood the whole world.
    if (templateGo.activeInHierarchy && !_templateSampleHidden)
    {
        MuteTemplateVisual(templateGo);
        _templateSampleHidden = true;
    }
}

private string StripUnitySuffixRuntime(string s)
{
    if (string.IsNullOrEmpty(s)) return "";
    s = s.Trim();
    int p = s.LastIndexOf('(');
    if (p > 0 && s.EndsWith(")"))
    {
        if (s[p - 1] == ' ')
            s = s.Substring(0, p - 1).Trim();
    }
    return s;
}

private Transform FindTemplateUnder(Transform root, string toolIdNorm)
{
    if (root == null) return null;
    Transform[] all = root.GetComponentsInChildren<Transform>(true);
    if (all == null) return null;

    for (int i = 0; i < all.Length; i++)
    {
        Transform t = all[i];
        if (t == null || t == root) continue;
        string n = NormalizeIdRuntime(StripUnitySuffixRuntime(t.name));
        if (n == toolIdNorm) return t;

        // common suffix variants
        string n2 = NormalizeIdRuntime(StripUnitySuffixRuntime(t.name) + "_PICKUP");
        if (n2 == toolIdNorm) return t;
    }
    return null;
}

private Transform FindToolTemplate(string toolId)
{
    string norm = NormalizeIdRuntime(toolId);
    if (string.IsNullOrEmpty(norm)) return null;

    Transform root = null;
    if (runtimeToolSpawner != null && runtimeToolSpawner.toolTemplatesRoot != null)
        root = runtimeToolSpawner.toolTemplatesRoot;

    if (root == null)
    {
        GameObject g = GameObject.Find("Tool");
        if (g == null) g = GameObject.Find("Tools");
        if (g == null) g = GameObject.Find("ToolTemplates");
        if (g != null) root = g.transform;
    }

    Transform tFound = FindTemplateUnder(root, norm);
    if (tFound != null) return tFound;

    // fallback: search toolModelsRoot if configured
    if (toolModelsRoot != null)
    {
        Transform f2 = FindTemplateUnder(toolModelsRoot, norm);
        if (f2 != null) return f2;
    }

    return null;
}

private bool IsWireMaterial(Material m)
{
    if (m == null) return false;
    string n = m.name == null ? "" : m.name.ToUpperInvariant();
    if (n.Contains("WIREFRAME") || n.Contains("WIREFRAMEFX")) return true;
    Shader sh = m.shader;
    if (sh != null)
    {
        string sn = sh.name == null ? "" : sh.name.ToUpperInvariant();
        if (sn.Contains("WIREFRAME")) return true;
    }
    return false;
}

private bool IsGlassMaterial(Material m)
{
    if (m == null) return false;
    string n = m.name == null ? "" : m.name.ToUpperInvariant();
    if (n.Contains("GLASSMASTER") || (n.Contains("GLASS") && !n.Contains("WIREFRAME"))) return true;
    Shader sh = m.shader;
    if (sh != null)
    {
        string sn = sh.name == null ? "" : sh.name.ToUpperInvariant();
        if (sn.Contains("GLASS")) return true;
    }
    return false;
}

private void CachePreviewMaterialsIfNeeded(Transform searchRoot)
{
    if (glassMasterMaterial != null && wireframeFxMaterial != null) return;
    if (searchRoot == null) return;

    Renderer[] rs = searchRoot.GetComponentsInChildren<Renderer>(true);
    if (rs == null) return;

    for (int i = 0; i < rs.Length; i++)
    {
        Renderer r = rs[i];
        if (r == null) continue;
        Material[] ms = r.sharedMaterials;
        if (ms == null) continue;

        for (int j = 0; j < ms.Length; j++)
        {
            Material m = ms[j];
            if (m == null) continue;

            if (glassMasterMaterial == null && IsGlassMaterial(m)) glassMasterMaterial = m;
            if (wireframeFxMaterial == null && IsWireMaterial(m)) wireframeFxMaterial = m;

            if (glassMasterMaterial != null && wireframeFxMaterial != null) return;
        }
    }
}

private void TrySetFloat(Material m, string prop, float value)
{
    if (m == null) return;
    if (!m.HasProperty(prop)) return;
    m.SetFloat(prop, value);
}

private void TrySetColorAlphaMin(Material m, string prop, float alphaMin)
{
    if (m == null) return;
    if (!m.HasProperty(prop)) return;
    Color c = m.GetColor(prop);
    if (c.a < alphaMin)
    {
        c.a = alphaMin;
        m.SetColor(prop, c);
    }
}

private void MakeGlassVisible(Material m)
{
    if (m == null) return;

    // Common color/opacity properties across shaders
    TrySetColorAlphaMin(m, "_MainColor", 0.08f);
    TrySetColorAlphaMin(m, "_BaseColor", 0.08f);
    TrySetColorAlphaMin(m, "_Color", 0.08f);
    TrySetColorAlphaMin(m, "_GlassTint", 0.08f);

    if (m.HasProperty("_Opacity"))
    {
        float o = m.GetFloat("_Opacity");
        if (o < 0.08f) m.SetFloat("_Opacity", 0.08f);
    }
    if (m.HasProperty("_Alpha"))
    {
        float a = m.GetFloat("_Alpha");
        if (a < 0.08f) m.SetFloat("_Alpha", 0.08f);
    }
}

private Material GetWireMaterialRuntime()
{
    // Tool-button preview uses WireframeFX when available.
    if (wireframeFxMaterial != null) return wireframeFxMaterial;

    // Fallback: GlassMaster (shared). Udon does NOT allow new Material(...)
    if (_runtimeFallbackVisibleGlassMaterial != null) return _runtimeFallbackVisibleGlassMaterial;
    if (glassMasterMaterial == null) return null;

    MakeGlassVisible(glassMasterMaterial);
    _runtimeFallbackVisibleGlassMaterial = glassMasterMaterial;
    return _runtimeFallbackVisibleGlassMaterial;
}

private Material GetElementGlassMaterialRuntime()
{
    // Element-button preview should be transparent (no wireframe).
    if (_runtimeElementGlassMaterial != null) return _runtimeElementGlassMaterial;

    // Prefer GlassMaster when available.
    if (glassMasterMaterial != null)
    {
        MakeGlassVisible(glassMasterMaterial);
        _runtimeElementGlassMaterial = glassMasterMaterial;
        return _runtimeElementGlassMaterial;
    }

    // Last resort: reuse WireframeFX but disable wire globally on the shared material.
    if (wireframeFxMaterial != null)
    {
        TrySetFloat(wireframeFxMaterial, "_WireEnable", 0f);
        TrySetFloat(wireframeFxMaterial, "_WireOpacity", 0f);
        TrySetFloat(wireframeFxMaterial, "_WireThickness", 0f);
        TrySetFloat(wireframeFxMaterial, "_WireWidth", 0f);
        TrySetFloat(wireframeFxMaterial, "_WireWidthPx", 0f);
        TrySetFloat(wireframeFxMaterial, "_ShowBase", 1f);
        MakeGlassVisible(wireframeFxMaterial);

        _runtimeElementGlassMaterial = wireframeFxMaterial;
        return _runtimeElementGlassMaterial;
    }

    return null;
}

private void ApplyToolMaterialMode(GameObject toolGo, bool elementMode)
{
    if (toolGo == null) return;

    // IMPORTANT (Udon-safe):
    // - Do NOT create new Material()
    // - Do NOT mutate shared materials globally
    // We instead use Renderer.materials to get per-renderer instances, then tweak properties.

    Renderer[] rs = toolGo.GetComponentsInChildren<Renderer>(true);
    if (rs == null) return;

    for (int i = 0; i < rs.Length; i++)
    {
        Renderer r = rs[i];
        if (r == null) continue;

        // Don't overwrite particle materials (VFX are handled separately)
        if (r.GetComponent<ParticleSystemRenderer>() != null) continue;

        Material[] ms = r.materials; // per-renderer instances
        if (ms == null || ms.Length == 0) continue;

        for (int j = 0; j < ms.Length; j++)
        {
            Material m = ms[j];
            if (m == null) continue;

            if (elementMode)
            {
                // Element mode: transparent glass, no wireframe
                if (m.HasProperty("_WireEnable")) m.SetFloat("_WireEnable", 0f);
                if (m.HasProperty("_WireOpacity")) m.SetFloat("_WireOpacity", 0f);
                if (m.HasProperty("_WireThickness")) m.SetFloat("_WireThickness", 0f);
                if (m.HasProperty("_WireWidth")) m.SetFloat("_WireWidth", 0f);
                if (m.HasProperty("_WireWidthPx")) m.SetFloat("_WireWidthPx", 0f);
                if (m.HasProperty("_ShowBase")) m.SetFloat("_ShowBase", 1f);

                MakeGlassVisible(m);
                if (m.HasProperty("_Opacity"))
                {
                    float o = m.GetFloat("_Opacity");
                    if (o < 0.08f) m.SetFloat("_Opacity", 0.08f);
                }
            }
            else
            {
                // Tool mode: keep wireframe if the shader supports it
                if (m.HasProperty("_WireEnable")) m.SetFloat("_WireEnable", 1f);
                if (m.HasProperty("_WireOpacity"))
                {
                    float o = m.GetFloat("_WireOpacity");
                    if (o <= 0f) m.SetFloat("_WireOpacity", 1f);
                }
                if (m.HasProperty("_WireThickness"))
                {
                    float t = m.GetFloat("_WireThickness");
                    if (t <= 0f) m.SetFloat("_WireThickness", 1f);
                }
            }
        }

        r.materials = ms;
        r.enabled = true;
    }
}

private Transform FindChildByNameRuntime(Transform root, string childName)
{
    if (root == null || string.IsNullOrEmpty(childName)) return null;
    Transform[] all = root.GetComponentsInChildren<Transform>(true);
    if (all == null) return null;
    for (int i = 0; i < all.Length; i++)
    {
        Transform t = all[i];
        if (t == null) continue;
        if (t.name == childName) return t;
    }
    return null;
}


private GameObject ResolveSampleVisualTemplateRuntime()
{
    if (_cachedSampleVisualTemplateSearched) return _cachedSampleVisualTemplateGo;
    _cachedSampleVisualTemplateSearched = true;

    // Prefer the in-scene template named "SampleVisual" (has backing UdonBehaviour and VFX rig)
    GameObject svGo = GameObject.Find("SampleVisual");
    if (svGo != null)
    {
        // Important: this is a scene-fixed template. Mute it so it doesn't emit in the scene.
        HideTemplateVisualIfInHierarchy(svGo);
        _cachedSampleVisualTemplateGo = svGo;
        return _cachedSampleVisualTemplateGo;
    }

    // Next: whatever is assigned to sampleVisual
    if (sampleVisual != null && sampleVisual.gameObject != null)
    {
        _cachedSampleVisualTemplateGo = sampleVisual.gameObject;
        return _cachedSampleVisualTemplateGo;
    }

    // Fallback: prefab list (ElementVisual)
    GameObject prefab = GetToolPrefabByIdRuntime("ElementVisual");
    if (prefab != null)
    {
        _cachedSampleVisualTemplateGo = prefab;
        return _cachedSampleVisualTemplateGo;
    }

    return null;
}

private GameObject ResolveReactionVfxTemplate()
{
    if (reactionVfxTemplate != null) return reactionVfxTemplate;

    Transform table = ResolveExperimentTableRoot();
    if (table == null) return null;

    Transform effects = FindChildByNameRuntime(table, "Effects");
    if (effects == null) effects = table;

    Transform vfx = FindChildByNameRuntime(effects, "ReactionVFX");
    if (vfx != null)
    {
        reactionVfxTemplate = vfx.gameObject;
        // keep template hidden to avoid double visuals
        if (reactionVfxTemplate.activeSelf) reactionVfxTemplate.SetActive(false);
    }
    return reactionVfxTemplate;
}

private void AttachReactionVfxClone(GameObject toolGo)
{
    if (toolGo == null) return;

    GameObject tpl = ResolveReactionVfxTemplate();
    if (tpl == null) return;

    GameObject vfxGo = VRCInstantiate(tpl);
    if (vfxGo == null) return;

    Transform anchor = FindChildByNameRuntime(toolGo.transform, reactionVfxAnchorName);
    if (anchor == null) anchor = toolGo.transform;

    vfxGo.transform.SetParent(anchor, false);
    vfxGo.transform.localPosition = Vector3.zero;
    vfxGo.transform.localRotation = Quaternion.identity;
    // Compensate anchor scaling so VFX stays visible under scaled tools
    Vector3 desiredWorld = (tpl != null) ? tpl.transform.lossyScale : Vector3.one;
    if (desiredWorld.x < 0.05f && desiredWorld.y < 0.05f && desiredWorld.z < 0.05f) desiredWorld = Vector3.one;
    Vector3 aScale = anchor.lossyScale;
    Vector3 ls = vfxGo.transform.localScale;
    ls.x = (aScale.x != 0f) ? (desiredWorld.x / aScale.x) : ls.x;
    ls.y = (aScale.y != 0f) ? (desiredWorld.y / aScale.y) : ls.y;
    ls.z = (aScale.z != 0f) ? (desiredWorld.z / aScale.z) : ls.z;
    vfxGo.transform.localScale = ls;

    if (!vfxGo.activeSelf) vfxGo.SetActive(true);

    // Ensure VFX is actually visible (templates sometimes ship with disabled children/renderers)
    ForceActiveDescendants(vfxGo.transform, 256);
    EnableAllRenderers(vfxGo);
    PlayAllParticles(vfxGo, true);
    if (forceVisibleOnSelect) ForceVisibleHierarchy(vfxGo.transform);

    // If the spawner has a reactionAnimator reference, point it to this instance (per-tool)
    ChemReactionAnimator anim = vfxGo.GetComponent<ChemReactionAnimator>();
    if (anim != null)
    {
        reactionAnimator = anim;
    }
}

private void AttachElementVisualClone(GameObject toolGo, string elementSymbol)
{
    if (toolGo == null) return;
    if (elementDb == null) return;

    GameObject templateGo = ResolveSampleVisualTemplateRuntime();

    // Fallback to the old ElementVisual prefab if SampleVisual is missing
    if (templateGo == null)
    {
        GameObject visPrefab = GetToolPrefabByIdRuntime("ElementVisual");
        if (visPrefab != null) templateGo = visPrefab;
        else if (sampleVisual != null) templateGo = sampleVisual.gameObject;
    }

    if (templateGo == null)
    {
        Debug.LogWarning("[ChemElementSpawner] Element visual template missing (SampleVisual / ElementVisual).");
        return;
    }

    // Scene-fixed template (ExperimentTable/VR_StartZone/ElementEffectAnchor/SampleVisual) must be muted,
    // otherwise it will keep emitting and "overflow" the whole world.
    GameObject visGo = null;

    Transform anchor = FindChildByNameRuntime(toolGo.transform, elementEffectAnchorName);
    if (anchor == null) anchor = toolGo.transform;

    // If the tool already has an effect prefab under the anchor, reuse it (no instantiation).
    // This allows you to place the VFX prefab as a child of the tool in Unity (no extra scripts needed).
    GameObject existingEffect = null;
    if (anchor != null)
    {
        ChemVisualController existingVc = anchor.GetComponentInChildren<ChemVisualController>(true);
        if (existingVc != null) existingEffect = existingVc.gameObject;
        else
        {
            ParticleSystem existingPs = anchor.GetComponentInChildren<ParticleSystem>(true);
            if (existingPs != null) existingEffect = existingPs.gameObject;
        }
    }

    // Use existing effect if present; otherwise instantiate the template.
    if (existingEffect != null)
    {
        visGo = existingEffect;
        if (!visGo.activeSelf) visGo.SetActive(true);
    }
    else
    {
        HideTemplateVisualIfInHierarchy(templateGo);

        visGo = VRCInstantiate(templateGo);
        if (visGo == null)
        {
            Debug.LogWarning("[ChemElementSpawner] VRCInstantiate failed for element visual.");
            return;
        }
    }



    visGo.transform.SetParent(anchor, false);
    visGo.transform.localRotation = Quaternion.identity;

    // IMPORTANT:
    // Preserve a *reasonable WORLD scale* so the effect stays visible even when
    // the tool hierarchy is scaled (many lab tools are imported at 0.01).
    // We still contain the particles with VFXVolume + module clamping, so this won't "overflow".
    // Choose a reasonable *WORLD* scale for the visual.
    // Many lab tools are imported under a scaled parent (e.g. 0.01), so relying on the template lossyScale
    // can make the VFX tiny and effectively invisible.
    Vector3 targetWorldScale = elementEffectLocalScale;
    if (targetWorldScale.x == 0f && targetWorldScale.y == 0f && targetWorldScale.z == 0f)
        targetWorldScale = Vector3.one;

    // If the in-scene template is already at a reasonable world size, use it.
    if (templateGo != null)
    {
        Vector3 t = templateGo.transform.lossyScale;
        // Ignore very small templates (commonly under 0.01 roots)
        if (t.x >= 0.05f && t.y >= 0.05f && t.z >= 0.05f)
            targetWorldScale = t;
    }

    // NaN guard
    if (targetWorldScale.x != targetWorldScale.x) targetWorldScale = Vector3.one;

    Vector3 aScale = anchor.lossyScale;
    Vector3 localScale = visGo.transform.localScale;
    localScale.x = (aScale.x != 0f) ? (targetWorldScale.x / aScale.x) : localScale.x;
    localScale.y = (aScale.y != 0f) ? (targetWorldScale.y / aScale.y) : localScale.y;
    localScale.z = (aScale.z != 0f) ? (targetWorldScale.z / aScale.z) : localScale.z;
    visGo.transform.localScale = localScale;

    // Prefer VFXVolume(BoxCollider) under the anchor; it defines the container volume.
    BoxCollider vfxBox;
    bool hasBox = TryGetVfxVolumeBox(anchor, out vfxBox);

    // Place inside volume if possible
    Vector3 localPos = elementEffectLocalOffset;
    if (hasBox)
    {
        // BoxCollider center/size are in box local, but we want anchor local.
        // We'll compute the world center and bring it back to anchor local.
        Vector3 wCenter = vfxBox.transform.TransformPoint(vfxBox.center);
        Vector3 wSize = AbsVec3(vfxBox.transform.TransformVector(vfxBox.size));

        // Clamp the volume size to tool bounds if available.
        Vector3 capSizeFromBounds = Vector3.zero;
        Bounds tb;
        bool hasBounds = TryGetRenderableBounds(toolGo.transform, out tb);
        if (hasBounds)
        {
            capSizeFromBounds = AbsVec3(tb.size);
        }

        // FIX: Many prefabs ship with a tiny VFXVolume (e.g. 0.02m cube).
        // If the volume is suspiciously small, fall back to tool bounds (more reliable).
        bool vfxTiny = (wSize.x < 0.045f) || (wSize.y < 0.045f) || (wSize.z < 0.045f);
        if (vfxTiny && (capSizeFromBounds.x > 0f || capSizeFromBounds.y > 0f || capSizeFromBounds.z > 0f))
        {
            wCenter = tb.center;
            wSize = Vector3.Scale(capSizeFromBounds, effectBoundsScale);
        }
        else if (capSizeFromBounds.x > 0f || capSizeFromBounds.y > 0f || capSizeFromBounds.z > 0f)
        {
            // Clamp oversize volumes down to bounds.
            wSize.x = Mathf.Min(wSize.x, Mathf.Max(0.001f, capSizeFromBounds.x));
            wSize.y = Mathf.Min(wSize.y, Mathf.Max(0.001f, capSizeFromBounds.y));
            wSize.z = Mathf.Min(wSize.z, Mathf.Max(0.001f, capSizeFromBounds.z));
        }

        // Absolute hard cap (safety): never allow a container volume bigger than typical lab glass.
        float hardMax = 0.45f;
        wSize.x = Mathf.Min(Mathf.Max(0.001f, wSize.x), hardMax);
        wSize.y = Mathf.Min(Mathf.Max(0.001f, wSize.y), hardMax);
        wSize.z = Mathf.Min(Mathf.Max(0.001f, wSize.z), hardMax);

        // Choose a point within the box by fill height
        Vector3 wFill = wCenter;
        wFill.y = wCenter.y - (wSize.y * 0.5f) + (wSize.y * Mathf.Clamp01(effectFillHeight01));
        localPos = anchor.InverseTransformPoint(wFill);
    }
    else
    {
        Bounds tb;
        if (TryGetRenderableBounds(toolGo.transform, out tb))
        {
            Vector3 w = tb.center + effectWorldOffset;
            w.y = tb.min.y + tb.size.y * effectFillHeight01 + effectWorldOffset.y;
            localPos = anchor.InverseTransformPoint(w);
        }
    }
    visGo.transform.localPosition = localPos;

    if (!visGo.activeSelf) visGo.SetActive(true);

    // Make sure particles are fully constrained inside the container.
    // NOTE: Some prefabs have an incorrectly sized VFXVolume. We therefore clamp the volume to the tool's render bounds.
    Bounds toolB;
    Vector3 worldSize = Vector3.zero;
    if (TryGetRenderableBounds(toolGo.transform, out toolB))
    {
        worldSize = Vector3.Scale(toolB.size, effectBoundsScale);
    }

    if (hasBox)
    {
        ConstrainParticlesToVfxVolume(visGo, vfxBox, worldSize, toolB.center);
        ScaleDownVisualToFitVfxVolume(visGo, vfxBox, worldSize);
    }
    else if (worldSize.x > 0f || worldSize.y > 0f || worldSize.z > 0f)
    {
        ConstrainParticlesToWorldBox(visGo, toolB.center, worldSize);
    }

    // Ensure visibility
    ForceActiveDescendants(visGo.transform, 8192);
    EnableAllRenderers(visGo);
    ScheduleLatePlayEffect(visGo, true);
    if (forceVisibleOnSelect) ForceVisibleHierarchy(visGo.transform);

    // Apply element state/colors (works when the template has backing UdonBehaviour).
    ChemVisualController vc = visGo.GetComponent<ChemVisualController>();
    if (vc == null) vc = visGo.GetComponentInChildren<ChemVisualController>(true);

    if (vc != null)
    {
        vc.EnsureInitialized();
        vc.NotifyElementSelected(elementSymbol);
        vc.ApplyElementBySymbol(elementDb, elementSymbol, _syncedTempC);

        // Failsafe: if all three states are inactive, force a visible state.
        if ((vc.solidObj == null || !vc.solidObj.activeSelf) &&
            (vc.liquidObj == null || !vc.liquidObj.activeSelf) &&
            (vc.gasObj == null || !vc.gasObj.activeSelf))
        {
            vc.SetState(ChemElementState.Solid);
        }

        EnableAllRenderers(visGo);
        ScheduleLatePlayEffect(visGo, false);
        if (forceVisibleOnSelect) ForceVisibleHierarchy(visGo.transform);
    }
    else
    {
        // Very last-resort: manually enable something named Solid/Liquid/Gas (for ElementVisual prefab).
        Transform s = FindChildByNameRuntime(visGo.transform, "Solid");
        Transform l = FindChildByNameRuntime(visGo.transform, "Liquid");
        Transform g = FindChildByNameRuntime(visGo.transform, "Gas");

        if (s != null) s.gameObject.SetActive(false);
        if (l != null) l.gameObject.SetActive(false);
        if (g != null) g.gameObject.SetActive(false);

        if (s != null) s.gameObject.SetActive(true);
        else if (l != null) l.gameObject.SetActive(true);
        else if (g != null) g.gameObject.SetActive(true);
        EnableAllRenderers(visGo);
        if (forceVisibleOnSelect) ForceVisibleHierarchy(visGo.transform);
    }
}

// ============================
// VFX containment (guaranteed)
// ============================
private bool TryGetVfxVolumeBox(Transform anchor, out BoxCollider box)
{
    box = null;
    if (anchor == null) return false;
    Transform t = FindChildByNameRuntime(anchor, "VFXVolume");
    if (t == null) return false;
    box = t.GetComponent<BoxCollider>();
    return box != null;
}

private Vector3 AbsVec3(Vector3 v)
{
    if (v.x < 0f) v.x = -v.x;
    if (v.y < 0f) v.y = -v.y;
    if (v.z < 0f) v.z = -v.z;
    return v;
}

private void MuteTemplateVisual(GameObject templateGo)
{
    if (templateGo == null) return;

    // Stop ALL particles under the scene template so it doesn't flood the world.
    ParticleSystem[] ps = templateGo.GetComponentsInChildren<ParticleSystem>(true);
    if (ps != null)
    {
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i] == null) continue;
            var em = ps[i].emission;
            em.enabled = false;
            ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // Hide renderers as well (some rigs include mesh/quad fog).
    Renderer[] rs = templateGo.GetComponentsInChildren<Renderer>(true);
    if (rs != null)
    {
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            rs[i].enabled = false;
        }
    }
}

/// <summary>
/// Some particle prefabs ship with ParticleSystemRenderer.material = None.
/// In that case, even if we set startColor / MPB, the VFX becomes invisible.
/// This method resolves a reasonable "master" particle material that already exists in the scene
/// (preferably the ChemLab particle shader) and we reuse it for missing particle renderers.
/// </summary>
private Material GetParticleMasterMaterial()
{
    if (_particleMasterMaterial != null) return _particleMasterMaterial;

    // 1) Prefer the SampleVisual template (even if muted/inactive)
    if (sampleVisual != null)
    {
        ParticleSystemRenderer[] prs = sampleVisual.GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (prs != null)
        {
            for (int i = 0; i < prs.Length; i++)
            {
                ParticleSystemRenderer pr = prs[i];
                if (pr == null) continue;
                Material m = pr.sharedMaterial;
                if (m == null) continue;
                string sn = (m.shader != null) ? m.shader.name : "";
                if (!string.IsNullOrEmpty(sn) && sn.IndexOf("ChemLab/Particle") >= 0)
                {
                    _particleMasterMaterial = m;
                    return _particleMasterMaterial;
                }
            }
            // fallback: any particle material under sampleVisual
            for (int i = 0; i < prs.Length; i++)
            {
                ParticleSystemRenderer pr = prs[i];
                if (pr == null) continue;
                Material m = pr.sharedMaterial;
                if (m == null) continue;
                _particleMasterMaterial = m;
                return _particleMasterMaterial;
            }
        }
    }

    // NOTE:
    // UdonSharp does not expose Object.FindObjectsOfType(Type).
    // We intentionally avoid a scene-wide scan here.
    // If you need a master particle material, assign one in the inspector
    // or ensure SampleVisual has a ParticleSystemRenderer with a material.

    // 2) Last resort: use an explicitly assigned fallback particle material.
    // IMPORTANT: UdonSharp cannot call Shader.Find at runtime, so we must rely on an inspector reference.
    if (particleFallbackMaterial != null)
    {
        _particleMasterMaterial = particleFallbackMaterial;
        return _particleMasterMaterial;
    }

    return null;
}

private void ConstrainParticlesToVfxVolume(GameObject visGo, BoxCollider vfxBox, Vector3 worldSize, Vector3 worldCenter)
{
    if (visGo == null || vfxBox == null) return;

    // Convert VFXVolume box into world-space center/size
    Vector3 wCenter = vfxBox.transform.TransformPoint(vfxBox.center);
    Vector3 wSize = AbsVec3(vfxBox.transform.TransformVector(vfxBox.size));

    // Clamp the volume size to tool bounds if provided (fixes prefabs where VFXVolume is accidentally huge)
    if (worldSize.x > 0f || worldSize.y > 0f || worldSize.z > 0f)
    {
        wSize.x = Mathf.Min(wSize.x, Mathf.Max(0.001f, worldSize.x));
        wSize.y = Mathf.Min(wSize.y, Mathf.Max(0.001f, worldSize.y));
        wSize.z = Mathf.Min(wSize.z, Mathf.Max(0.001f, worldSize.z));
    }

    // Absolute hard cap (safety): never allow a container volume bigger than typical lab glass, even if bounds are wrong.
    float hardMax = 0.45f;
    wSize.x = Mathf.Min(wSize.x, hardMax);
    wSize.y = Mathf.Min(wSize.y, hardMax);
    wSize.z = Mathf.Min(wSize.z, hardMax);

    float minDim = wSize.x;
    if (wSize.y < minDim) minDim = wSize.y;
    if (wSize.z < minDim) minDim = wSize.z;
    if (minDim < 0.001f) minDim = 0.001f;

    // FIX (2026-01): Many tool prefabs ship with a tiny VFXVolume (e.g. 0.02m cube).
    // In that case, particles become effectively invisible. If the volume is suspiciously small,
    // fall back to the tool render bounds size passed in via worldSize.
    bool vfxTooSmall = (wSize.x < 0.045f) || (wSize.y < 0.045f) || (wSize.z < 0.045f);
    bool hasCap = (worldSize.x > 0f) || (worldSize.y > 0f) || (worldSize.z > 0f);
    if (vfxTooSmall && hasCap)
    {
        wCenter = worldCenter;
        wSize = AbsVec3(worldSize);

        // Recompute minDim with fallback size
        minDim = wSize.x;
        if (wSize.y < minDim) minDim = wSize.y;
        if (wSize.z < minDim) minDim = wSize.z;
        if (minDim < 0.001f) minDim = 0.001f;
    }


    ParticleSystem[] ps = visGo.GetComponentsInChildren<ParticleSystem>(true);
    if (ps == null) return;

    for (int i = 0; i < ps.Length; i++)
    {
        ParticleSystem p = ps[i];
        if (p == null) continue;

        // Ensure local simulation so it stays inside the container transform.
        var main = p.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 2000;

        // Map the box into THIS particle system's local space
        Vector3 localCenter = p.transform.InverseTransformPoint(wCenter);
        Vector3 localSize = AbsVec3(p.transform.InverseTransformVector(wSize));

        // Hard-shape clamp
        var shape = p.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = localCenter;
        shape.scale = localSize;

        // Kill outward push modules that cause "overflow"
        var vel = p.velocityOverLifetime; vel.enabled = false;
        var force = p.forceOverLifetime; force.enabled = false;
        var noise = p.noise; noise.enabled = false;
        var inherit = p.inheritVelocity; inherit.enabled = false;
        var ext = p.externalForces; ext.enabled = false;

        // Clamp emission to something that cannot blanket the scene
        var em = p.emission;
        em.enabled = true;
        float maxRate = 250f; // safe upper bound
        if (minDim < 0.15f) maxRate = 120f;
        if (minDim < 0.08f) maxRate = 60f;
        var rate = em.rateOverTime;
        float r = rate.constant;
        if (r <= 0f) r = maxRate * 0.5f;
        if (r > maxRate) r = maxRate;
        em.rateOverTime = r;

        // Lifetime + speed clamp so particles die before reaching outside volume
        float maxSpeed = minDim * 0.65f;
        if (maxSpeed < 0.05f) maxSpeed = 0.05f;
        if (main.startSpeed.constant > maxSpeed) main.startSpeed = maxSpeed;

        float maxLife = 0.9f;
        if (minDim < 0.15f) maxLife = 0.6f;
        if (main.startLifetime.constant > maxLife) main.startLifetime = maxLife;

        float maxSize = minDim * 0.18f;
        if (maxSize < 0.01f) maxSize = 0.01f;
        if (main.startSize.constant > maxSize) main.startSize = maxSize;

        // Limit velocity as a final failsafe
        var limit = p.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = maxSpeed;
        limit.dampen = 0.75f;

        // Additional clamp: ensure travel distance stays within the volume
        float safeTravel = minDim * 0.45f;
        float spd = main.startSpeed.constant;
        float life = main.startLifetime.constant;
        if (spd > 0.001f)
        {
            float maxTravel = spd * life;
            if (maxTravel > safeTravel)
            {
                life = Mathf.Max(0.1f, safeTravel / spd);
                main.startLifetime = Mathf.Min(life, maxLife);
            }
        }

        // Shader-side clip box (particles remain invisible outside the glassware)
        ParticleSystemRenderer pr = p.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            // Fix: particles can be invisible OR ignore clipping if they don't use the ChemLab particle shader.
            // Always enforce a ChemLab/Particle* material when possible.
            bool needMat = (pr.sharedMaterial == null);
            if (!needMat)
            {
                Shader sh = (pr.sharedMaterial != null) ? pr.sharedMaterial.shader : null;
                string sn = (sh != null) ? sh.name : "";
                if (string.IsNullOrEmpty(sn) || sn.IndexOf("ChemLab/Particle") < 0) needMat = true;
                else if (!pr.sharedMaterial.HasProperty("_UseClip")) needMat = true;
            }

            if (needMat)
            {
                Material pm = GetParticleMasterMaterial();
                if (pm != null) pr.sharedMaterial = pm;
            }

            if (_particleMpb == null) _particleMpb = new MaterialPropertyBlock();
            pr.GetPropertyBlock(_particleMpb); // [ChemLabFix] Preserve MPB (keep element color)
            
            _particleMpb.SetFloat("_UseClip", 1f);
            _particleMpb.SetVector("_ClipCenter", new Vector4(wCenter.x, wCenter.y, wCenter.z, 0f));
            _particleMpb.SetVector("_ClipExtents", new Vector4(wSize.x * 0.5f, wSize.y * 0.5f, wSize.z * 0.5f, 0f));
            pr.SetPropertyBlock(_particleMpb);
        }
    }
}

private void ConstrainParticlesToWorldBox(GameObject visGo, Vector3 worldCenter, Vector3 worldSize)
{
    if (visGo == null) return;

    Vector3 wCenter = worldCenter;

    Vector3 wSize = AbsVec3(worldSize);
    float hardMax = 0.45f;
    wSize.x = Mathf.Min(Mathf.Max(0.001f, wSize.x), hardMax);
    wSize.y = Mathf.Min(Mathf.Max(0.001f, wSize.y), hardMax);
    wSize.z = Mathf.Min(Mathf.Max(0.001f, wSize.z), hardMax);

    float minDim = wSize.x;
    if (wSize.y < minDim) minDim = wSize.y;
    if (wSize.z < minDim) minDim = wSize.z;
    if (minDim < 0.001f) minDim = 0.001f;

    // FIX (2026-01): Many tool prefabs ship with a tiny VFXVolume (e.g. 0.02m cube).
    // In that case, particles become effectively invisible. If the volume is suspiciously small,
    // fall back to the tool render bounds size passed in via worldSize.
    bool vfxTooSmall = (wSize.x < 0.045f) || (wSize.y < 0.045f) || (wSize.z < 0.045f);
    bool hasCap = (worldSize.x > 0f) || (worldSize.y > 0f) || (worldSize.z > 0f);
    if (vfxTooSmall && hasCap)
    {
        wCenter = worldCenter;
        wSize = AbsVec3(worldSize);

        // Recompute minDim with fallback size
        minDim = wSize.x;
        if (wSize.y < minDim) minDim = wSize.y;
        if (wSize.z < minDim) minDim = wSize.z;
        if (minDim < 0.001f) minDim = 0.001f;
    }


    ParticleSystem[] ps = visGo.GetComponentsInChildren<ParticleSystem>(true);
    if (ps == null) return;

    for (int i = 0; i < ps.Length; i++)
    {
        ParticleSystem p = ps[i];
        if (p == null) continue;

        var main = p.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 2000;

        Vector3 localCenter = p.transform.InverseTransformPoint(worldCenter);
        Vector3 localSize = AbsVec3(p.transform.InverseTransformVector(wSize));

        var shape = p.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = localCenter;
        shape.scale = localSize;

        var vel = p.velocityOverLifetime; vel.enabled = false;
        var force = p.forceOverLifetime; force.enabled = false;
        var noise = p.noise; noise.enabled = false;
        var inherit = p.inheritVelocity; inherit.enabled = false;
        var ext = p.externalForces; ext.enabled = false;

        var em = p.emission;
        em.enabled = true;
        float maxRate = 200f;
        if (minDim < 0.15f) maxRate = 100f;
        if (minDim < 0.08f) maxRate = 50f;
        var rate = em.rateOverTime;
        float r = rate.constant;
        if (r <= 0f) r = maxRate * 0.5f;
        if (r > maxRate) r = maxRate;
        em.rateOverTime = r;

        float maxSpeed = minDim * 0.65f;
        if (maxSpeed < 0.05f) maxSpeed = 0.05f;
        if (main.startSpeed.constant > maxSpeed) main.startSpeed = maxSpeed;

        float maxLife = 0.8f;
        if (minDim < 0.15f) maxLife = 0.55f;
        if (main.startLifetime.constant > maxLife) main.startLifetime = maxLife;

        float maxSize = minDim * 0.18f;
        if (maxSize < 0.01f) maxSize = 0.01f;
        if (main.startSize.constant > maxSize) main.startSize = maxSize;

        var limit = p.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = maxSpeed;
        limit.dampen = 0.75f;

        float safeTravel = minDim * 0.45f;
        float spd = main.startSpeed.constant;
        float life = main.startLifetime.constant;
        if (spd > 0.001f)
        {
            float maxTravel = spd * life;
            if (maxTravel > safeTravel)
            {
                life = Mathf.Max(0.1f, safeTravel / spd);
                main.startLifetime = Mathf.Min(life, maxLife);
            }
        }

        ParticleSystemRenderer pr = p.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            // Fix: enforce ChemLab particle shader so world-box clipping always works.
            bool needMat = (pr.sharedMaterial == null);
            if (!needMat)
            {
                Shader sh = (pr.sharedMaterial != null) ? pr.sharedMaterial.shader : null;
                string sn = (sh != null) ? sh.name : "";
                if (string.IsNullOrEmpty(sn) || sn.IndexOf("ChemLab/Particle") < 0) needMat = true;
                else if (!pr.sharedMaterial.HasProperty("_UseClip")) needMat = true;
            }

            if (needMat)
            {
                Material pm = GetParticleMasterMaterial();
                if (pm != null) pr.sharedMaterial = pm;
            }

            if (_particleMpb == null) _particleMpb = new MaterialPropertyBlock();
            pr.GetPropertyBlock(_particleMpb); // [ChemLabFix] Preserve MPB (keep element color)
            
            _particleMpb.SetFloat("_UseClip", 1f);
            _particleMpb.SetVector("_ClipCenter", new Vector4(worldCenter.x, worldCenter.y, worldCenter.z, 0f));
            _particleMpb.SetVector("_ClipExtents", new Vector4(wSize.x * 0.5f, wSize.y * 0.5f, wSize.z * 0.5f, 0f));
            pr.SetPropertyBlock(_particleMpb);
        }
    }
}

private void ScaleDownVisualToFitVfxVolume(GameObject visGo, BoxCollider vfxBox, Vector3 worldSize)
{
    if (visGo == null || vfxBox == null) return;

    Renderer[] rs = visGo.GetComponentsInChildren<Renderer>(true);
    if (rs == null || rs.Length == 0) return;

    // World bounds of visual
    Bounds b = rs[0].bounds;
    for (int i = 1; i < rs.Length; i++)
    {
        if (rs[i] == null) continue;
        b.Encapsulate(rs[i].bounds);
    }

    // World size of volume
    Vector3 wSize = AbsVec3(vfxBox.transform.TransformVector(vfxBox.size));

    // Clamp to tool bounds if provided
    if (worldSize.x > 0f || worldSize.y > 0f || worldSize.z > 0f)
    {
        wSize.x = Mathf.Min(wSize.x, Mathf.Max(0.001f, worldSize.x));
        wSize.y = Mathf.Min(wSize.y, Mathf.Max(0.001f, worldSize.y));
        wSize.z = Mathf.Min(wSize.z, Mathf.Max(0.001f, worldSize.z));
    }

    // Absolute hard cap (safety)
    float hardMax = 0.45f;
    wSize.x = Mathf.Min(wSize.x, hardMax);
    wSize.y = Mathf.Min(wSize.y, hardMax);
    wSize.z = Mathf.Min(wSize.z, hardMax);

    wSize.x = Mathf.Max(0.001f, wSize.x);
    wSize.y = Mathf.Max(0.001f, wSize.y);
    wSize.z = Mathf.Max(0.001f, wSize.z);

    Vector3 visSize = b.size;
    visSize.x = Mathf.Max(0.001f, visSize.x);
    visSize.y = Mathf.Max(0.001f, visSize.y);
    visSize.z = Mathf.Max(0.001f, visSize.z);

    float fx = wSize.x / visSize.x;
    float fy = wSize.y / visSize.y;
    float fz = wSize.z / visSize.z;
    float f = fx;
    if (fy < f) f = fy;
    if (fz < f) f = fz;
    f *= 0.92f;

    if (f < 1f)
    {
        Vector3 s = visGo.transform.localScale;
        visGo.transform.localScale = new Vector3(s.x * f, s.y * f, s.z * f);
    }
}


private GameObject SpawnToolInstance(string toolId, bool toolButtonMode)
{
    // IMPORTANT: Do NOT call runtimeToolSpawner.InstantiateTool() here.
    // That method can be configured to destroy/replace previous instances.
    // We want unlimited spawns per button press, so we instantiate prefabs directly.
    Transform template = null;
    GameObject go = null;

    // Prefer cloning an in-scene template (has correct renderers/colliders/Udon backing).
    // Prefab assets can be missing backing UdonBehaviours in some setups.
    template = FindToolTemplate(toolId);
    if (template != null)
    {
        go = VRCInstantiate(template.gameObject);
    }

    // Fallback: prefab list (runtimeToolSpawner.toolPrefabs)
    GameObject prefab = null;
    if (go == null)
    {
        prefab = GetToolPrefabByIdRuntime(toolId);
        if (prefab != null)
            go = VRCInstantiate(prefab);
    }

    if (go == null)
    {
        Debug.LogWarning("[ChemElementSpawner] SpawnToolInstance failed. Tool template/prefab not found: " + toolId);
        return null;
    }

    if (go == null)
    {
        Debug.LogWarning("[ChemElementSpawner] SpawnToolInstance failed. VRCInstantiate returned null for: " + toolId);
        return null;
    }

    _runtimeSpawnSerial++;
    string baseName = (template != null) ? template.name : (prefab != null ? prefab.name : toolId);
    go.name = baseName + "_RUNTIME_" + _runtimeSpawnSerial;

    Transform parent = ResolveRuntimeSpawnParent();
    if (parent != null) go.transform.SetParent(parent, true);

    // Place it on top of the table/floor via raycast (prevents sinking / weird intersections)
    go.transform.position = GetRandomSpawnPosition();

    // Preserve template world rotation when cloning from a scene template.
    if (template != null)
    {
        // do NOT overwrite with parent rotation
        go.transform.rotation = template.rotation;
    }

    // Preserve template world scale under parent (prevents "shape broken" when parent scale != 1)
    // Template mode only (prefabs have no scene lossyScale reference).
    if (parent != null && template != null)
    {
        Vector3 tScale = template.lossyScale;
        Vector3 pScale = parent.lossyScale;
        Vector3 local = go.transform.localScale;
        local.x = (pScale.x != 0f) ? (tScale.x / pScale.x) : local.x;
        local.y = (pScale.y != 0f) ? (tScale.y / pScale.y) : local.y;
        local.z = (pScale.z != 0f) ? (tScale.z / pScale.z) : local.z;
        go.transform.localScale = local;
    }

    if (!go.activeSelf) go.SetActive(true);

    // Force all descendants active (templates sometimes keep render children disabled).
    ForceActiveDescendants(go.transform, 4096);

    if (forceVisibleOnSelect) ForceVisibleHierarchy(go.transform);

    // tool button => wireframe mode; element mode handled elsewhere
    ApplyToolMaterialMode(go, false);

    // Attach per-tool reaction VFX clone
    AttachReactionVfxClone(go);

    // IMPORTANT:
    // Runtime-spawned tools must NOT behave like UI buttons.
    // Some in-scene templates are also used as selector buttons (SpawnSelectorButton / SelectorObject / etc.).
    // If those scripts remain enabled on the runtime clone, clicking the spawned object will trigger
    // SelectElement/SelectEquipment again, causing infinite spawning.
    DisableRuntimeSpawnInteractions(go);

    RegisterRuntimeSpawn(go);

    return go;
}

private void DisableRuntimeSpawnInteractions(GameObject root)
{
    if (root == null) return;

    // Runtime-spawned objects should be *pickable*, but must NOT behave like UI.
    // Some templates are shared with UI buttons. If those scripts remain active, clicking the spawned object
    // will run selection/spawn logic again (appears as "it duplicated" or "teleported" to a random spot).
    //
    // Important: In VRChat/Udon, disabling a behaviour is not always enough to prevent Interact() from firing
    // in edge cases (depending on how the interaction is routed). Therefore we also clear references/commands.

    SpawnSelectorButton[] spawnBtns = root.GetComponentsInChildren<SpawnSelectorButton>(true);
    for (int i = 0; i < spawnBtns.Length; i++)
    {
        SpawnSelectorButton b = spawnBtns[i];
        if (b == null) continue;
        b.idOrName = "";
        b.elementSpawner = null;
        b.environmentManager = null;
        b.statusDisplay = null;
        b.enabled = false;
    }

    SelectorObject[] selectorObjs = root.GetComponentsInChildren<SelectorObject>(true);
    for (int i = 0; i < selectorObjs.Length; i++)
    {
        SelectorObject s = selectorObjs[i];
        if (s == null) continue;
        s.selected = null;
        s.idOverride = "";
        s.parentToZoneOnSelect = false;
        s.zoneForThisCategory = null;
        s.enabled = false;
    }

    SelectionActionController[] actionCtrls = root.GetComponentsInChildren<SelectionActionController>(true);
    for (int i = 0; i < actionCtrls.Length; i++)
    {
        SelectionActionController a = actionCtrls[i];
        if (a == null) continue;
        a.buttons = null;
        a.enabled = false;
    }

    ValueAdjustButton[] valueBtns = root.GetComponentsInChildren<ValueAdjustButton>(true);
    for (int i = 0; i < valueBtns.Length; i++)
    {
        ValueAdjustButton v = valueBtns[i];
        if (v == null) continue;
        v.env = null;
        v.command = "";
        v.enabled = false;
    }

    StartExperimentButton[] startBtns = root.GetComponentsInChildren<StartExperimentButton>(true);
    for (int i = 0; i < startBtns.Length; i++)
    {
        StartExperimentButton st = startBtns[i];
        if (st == null) continue;
        st.spawner = null;
        st.enabled = false;
    }

    ResetExperimentButton[] resetBtns = root.GetComponentsInChildren<ResetExperimentButton>(true);
    for (int i = 0; i < resetBtns.Length; i++)
    {
        ResetExperimentButton r = resetBtns[i];
        if (r == null) continue;
        r.spawner = null;
        r.envManager = null;
        r.uiSync = null;
        r.enabled = false;
    }

    OperatorButton[] opBtns = root.GetComponentsInChildren<OperatorButton>(true);
    for (int i = 0; i < opBtns.Length; i++)
    {
        OperatorButton o = opBtns[i];
        if (o == null) continue;
        o.spawner = null;
        o.mode = "";
        o.enabled = false;
    }

    // UI folder scripts (may exist on templates)
    ConditionAdjuster[] condAdj = root.GetComponentsInChildren<ConditionAdjuster>(true);
    for (int i = 0; i < condAdj.Length; i++)
    {
        ConditionAdjuster c = condAdj[i];
        if (c == null) continue;
        c.env = null;
        c.spawner = null;
        c.command = "";
        c.enabled = false;
    }

    // Build/runtime fallback:
    // Some UI button scripts may appear as plain UdonBehaviours in Udon runtime.
    // Avoid using GetProgramVariable() for detection (it logs errors when the variable doesn't exist).
    // Instead, disable UdonBehaviours/colliders that live under obvious UI-like transforms.
    DisableUiLikeUdonBehaviours(root);
}

private void DisableUiLikeUdonBehaviours(GameObject root)
{
    if (root == null) return;

    UdonBehaviour[] ubs = root.GetComponentsInChildren<UdonBehaviour>(true);
    for (int i = 0; i < ubs.Length; i++)
    {
        UdonBehaviour ub = ubs[i];
        if (ub == null) continue;
        if (!IsUiLikeTransform(ub.transform)) continue;
        ub.enabled = false;
    }

    // Desktop click still triggers Interact on colliders even if the behaviour is disabled in some cases,
    // so also disable colliders that belong to UI-like nodes.
    Collider[] cols = root.GetComponentsInChildren<Collider>(true);
    for (int i = 0; i < cols.Length; i++)
    {
        Collider c = cols[i];
        if (c == null) continue;
        if (!IsUiLikeTransform(c.transform)) continue;
        c.enabled = false;
    }
}

private void SpawnElementContainerInstance(string elementSymbol)
{
    // Spawn a new container (conical flask by default) and apply element visuals into it.
    // elementContainerToolId is a newly-added field in this branch and may be empty on existing scenes,
    // so fall back to the already-existing autoBeakerToolId / CONICAL_FLASK.
    string containerId = elementContainerToolId;
    if (string.IsNullOrEmpty(containerId)) containerId = autoBeakerToolId;
    if (string.IsNullOrEmpty(containerId)) containerId = "CONICAL_FLASK";

    GameObject go = SpawnToolInstance(containerId, false);
    if (go == null) return;

    // Element button => Glass + element visual ON
    ApplyToolMaterialMode(go, true);
    AttachElementVisualClone(go, elementSymbol);

    // Also point current selection for experiment start (optional)
    _syncedTool = containerId;
}

}