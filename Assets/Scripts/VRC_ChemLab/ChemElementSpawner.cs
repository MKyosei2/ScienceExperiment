using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

/// <summary>
/// ChemElementSpawner (Network-authoritative experiment core)
/// ---------------------------------------------------------
/// 同期：実験の真実（操作者/選択/環境/反応結果/進行度）
/// 非同期：見た目（演出/補間/粒子/移動）
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
    public ChemVisualController sampleVisual;       // 見た目（固液気/色/マテリアル）
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

    // -----------------------------
    // Synced experiment "truth"
    // -----------------------------
    [UdonSynced] private int _syncedVersion;              // 変更カウンタ
    [UdonSynced] private int _syncedOperatorPlayerId = -1;

    [UdonSynced] private string _syncedInput;            // 元素/式（ボタンのidOrName）
    [UdonSynced] private string _syncedTool;             // 器具ID
    [UdonSynced] private float _syncedTempC;
    [UdonSynced] private float _syncedPressureKPa;
    [UdonSynced] private float _syncedHumidity;

    [UdonSynced] private string _syncedReactionTag;      // "oxidation"/"chloride"/"none" etc.
    [UdonSynced] private string _syncedProductFormula;   // 生成物（式/ラベル）
    [UdonSynced] private int _syncedSeed;                // 再現性確保用

    [UdonSynced] private int _syncedPhase;               // 0=idle 1=running 2=complete
    [UdonSynced] private float _syncedProgress01;        // 0..1（演出を揃えたい場合）

    // -----------------------------
    // Local cache
    // -----------------------------
    private int _lastAppliedVersion = -1;
    private float _nextProgressSyncAt;
    private string _history = "";
    private string _localInput = "";
    private string _localTool = "";

    private void Start()
    {
        // 初回UI反映（ローカル）
        ApplyVisualFromState(force:true);
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
        _syncedVersion++;
        RequestSerialization();

        AppendHistory("ReleaseOperator");
        StopLocalSession();
        ApplyVisualFromState(force:true);
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

        ApplyVisualFromState(force:true);
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

    // 環境調整（ConditionAdjusterから呼ぶ想定）
    public void ModifyEnvironment(string command)
    {
        if (!EnsureCanControl()) return;
        if (environment == null) return;

        environment.Modify(command);

        // 同期へ反映
        _syncedTempC = environment.temperatureC;
        _syncedPressureKPa = environment.pressureKPa;
        _syncedHumidity = environment.humidity;

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("ModifyEnv: " + command);
        WriteUI();
    }

    // 実験開始（開始ボタン想定）
    public void _StartExperiment()
    {
        if (!EnsureCanControl()) return;

        // 環境値（確定値）を取得して同期に載せる
        PullEnvironmentForSync();

        // Seed（再現性）：入力/器具/環境からint範囲ハッシュ
        _syncedSeed = ComputeSeed(_syncedInput, _syncedTool, _syncedTempC, _syncedPressureKPa, _syncedHumidity);

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

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("StartExperiment: input=" + _syncedInput + " tool=" + _syncedTool + " tag=" + _syncedReactionTag);

        // ローカル：セッション開始（視覚/説明生成）
        StartLocalSessionFromSynced();

        ApplyVisualFromState(force:true);
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

        // envはデフォルトに戻す（環境マネージャ側でもResetされる想定）
        PullEnvironmentForSync();

        _syncedVersion++;
        RequestSerialization();

        AppendHistory("ResetExperiment");
        StopLocalSession();
        ApplyVisualFromState(force:true);
        WriteUI();
    }

    // =====================================================
    // Update loop (async visuals at 60fps, low-rate sync)
    // =====================================================
    private void Update()
    {
        // ローカル可視化は常時（60fps）
        TickLocalVisual(Time.deltaTime);

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
    }

    private void TickLocalVisual(float dt)
    {
        // operator：入力操作で進行（将来：手の動き/注ぎ/加熱を入れる）
        if (_syncedPhase == 1 && IsOperatorLocal())
        {
            if (ai != null)
            {
                // dt駆動（ローカル）。ここをVRモーションで増減する設計にできる。
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

    // =====================================================
    // Sync receive
    // =====================================================
    public override void OnDeserialization()
    {
        if (_syncedVersion == _lastAppliedVersion) return;
        _lastAppliedVersion = _syncedVersion;

        // ローカルキャッシュ更新
        _localInput = _syncedInput == null ? "" : _syncedInput;
        _localTool = _syncedTool == null ? "" : _syncedTool;

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

        ApplyVisualFromState(force:true);
        WriteUI();
    }

    // =====================================================
    // Helpers
    // =====================================================
    private void PullEnvironmentForSync()
    {
        if (environment != null)
        {
            _syncedTempC = environment.temperatureC;
            _syncedPressureKPa = environment.pressureKPa;
            _syncedHumidity = environment.humidity;
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
            explainGenerator.Generate(_syncedInput, _syncedTool, potential, _syncedTempC, _syncedPressureKPa, _syncedHumidity, dangerous,
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

        string sym = _syncedInput == null ? "" : _syncedInput;
        sampleVisual.ApplyElementBySymbol(elementDb, sym, _syncedTempC);
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

    // Udon互換：int内で回る簡易ハッシュ（mod演算はintのみ）
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

}
