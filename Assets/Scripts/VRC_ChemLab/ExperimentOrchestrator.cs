using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// ExperimentOrchestrator (教材ゲーム用ルート：システム重視)
/// - ミッション（課題）を同期
/// - 実験完了時に自動採点（同期）
/// - ヒント/振り返り（教育要素）を提供
/// 
/// 重要方針:
/// - 同期：ミッション番号、採点結果、スコア（真実）
/// - 非同期：UI表示、演出
/// </summary>
[AddComponentMenu("VRC Lab/ExperimentOrchestrator")]
public class ExperimentOrchestrator : UdonSharpBehaviour
{
    [Header("References")]
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager environmentManager;
    public EnvUISyncBridge uiSync;

    

    [Header("Presentation Lock (single experiment)")]
    [Tooltip("If true, mission is locked to the Hydrogen + Chlorine -> HCl presentation experiment.")]
    public bool forceSingleMission = true;

    [Tooltip("Forced goal product formula when forceSingleMission is true. Default: HCl")]
    public string forcedMissionGoalProductFormula = "HCl";

    [Tooltip("Overwrite mission[0] to the forced presentation mission on Start.")]
    public bool overwriteMissionZeroForPresentation = true;
[Header("Mission Data (can be overwritten via Importer)")]
    public string[] missionTitles;
    [TextArea] public string[] missionPrompts;
    public string[] missionGoalProductFormula; // 例: "H2O", "NaCl"
    public int[] missionPoints;

    [Header("Mission Conditions (Importer-friendly arrays)")]
    [Tooltip("必須器具ID。空欄なら制約なし。")]
    public string[] missionRequiredToolId;

    [Tooltip("温度条件（提出時の同期温度に対して適用）。max <= min の場合は無視。")]
    public float[] missionMinTempC;
    public float[] missionMaxTempC;

    [Tooltip("操作条件：実験中に到達した最大値で判定。0なら制約なし。")]
    public float[] missionMinHeat01;
    public float[] missionMinStir01;
    public float[] missionMinPour01;
    public float[] missionMinShake01;

    [Tooltip("1の場合、実験がComplete(phase=2)になってからでないとPASS不可")]
    public int[] missionRequireComplete;

    [Header("Settings")]
    public bool autoStartOnDesktop = false;
    public bool autoGradeOnComplete = true;
    public bool autoAdvanceOnSuccess = false;

    // ============ Synced "truth" ============
    [UdonSynced] private int _missionIndex = 0;
    [UdonSynced] private int _missionPhase = 0; // 0=briefing, 1=running, 2=graded
    [UdonSynced] private int _score = 0;
    [UdonSynced] private int _attempts = 0;
    [UdonSynced] private int _lastGrade = 0;   // 0=none, 1=pass, -1=fail

    // Local cache
    private int _lastSeenMissionIndex = -1;
    private int _lastSeenMissionPhase = -1;

    private void Start()
    {
        // PCのときだけ自動開始
        if (spawner == null) return;

        bool isVR = Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR();
        if (!isVR && autoStartOnDesktop)
        {
            spawner.SendCustomEvent("_StartExperiment");
        }

        EnsureMissionDefaults();
        ApplyPresentationMissionLock();
        RefreshAllDisplaysLocal();
    }

    // ============ Public getters for UI ============
    public int GetMissionIndex() { return _missionIndex; }
    public int GetScore() { return _score; }
    public int GetAttempts() { return _attempts; }

    public string GetMissionTitle()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionTitles == null) return "";
        if (_missionIndex >= missionTitles.Length) return "";
        return missionTitles[_missionIndex];
    }

    public string GetMissionGoal()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionGoalProductFormula == null) return "";
        if (_missionIndex >= missionGoalProductFormula.Length) return "";
        return missionGoalProductFormula[_missionIndex];
    }

    public string GetMissionRequiredTool()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionRequiredToolId == null) return "";
        if (_missionIndex >= missionRequiredToolId.Length) return "";
        return missionRequiredToolId[_missionIndex];
    }

    // Backward compatible name (used internally)
    public string GetMissionRequiredToolId() { return GetMissionRequiredTool(); }

    public float GetMissionMinTempC()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionMinTempC == null) return 0f;
        if (_missionIndex >= missionMinTempC.Length) return 0f;
        return missionMinTempC[_missionIndex];
    }

    public float GetMissionMaxTempC()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionMaxTempC == null) return 0f;
        if (_missionIndex >= missionMaxTempC.Length) return 0f;
        return missionMaxTempC[_missionIndex];
    }

    public float GetMissionMinHeat01() { return GetArrayF(missionMinHeat01); }
    public float GetMissionMinStir01() { return GetArrayF(missionMinStir01); }
    public float GetMissionMinPour01() { return GetArrayF(missionMinPour01); }
    public float GetMissionMinShake01() { return GetArrayF(missionMinShake01); }

    public bool GetMissionRequireComplete()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionRequireComplete == null) return false;
        if (_missionIndex >= missionRequireComplete.Length) return false;
        return missionRequireComplete[_missionIndex] != 0;
    }

    private float GetArrayF(float[] arr)
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || arr == null) return 0f;
        if (_missionIndex >= arr.Length) return 0f;
        return arr[_missionIndex];
    }

    public string GetMissionPrompt()
    {
        EnsureMissionDefaults();
        if (_missionIndex < 0 || missionPrompts == null) return "";
        if (_missionIndex >= missionPrompts.Length) return "";
        return missionPrompts[_missionIndex];
    }

    public string GetMissionPhaseText()
    {
        if (_missionPhase == 0) return "Briefing";
        if (_missionPhase == 1) return "Running";
        if (_missionPhase == 2) return "Graded";
        return "Unknown";
    }

    public string GetLastGradeText()
    {
        if (_lastGrade == 1) return "PASS";
        if (_lastGrade == -1) return "FAIL";
        return "-";
    }

    // ============ Interaction entry points (bind to cube buttons) ============
    public void _MissionStart()
    {
        if (!CanControlMission()) return;
        EnsureOwner();
        _missionPhase = 1;
        _attempts = 0;
        _lastGrade = 0;
        RequestSerialization();
        ResetExperimentLocal();
        RefreshAllDisplaysLocal();
    }

    public void _MissionNext()
    {
        if (forceSingleMission) return;
        if (!CanControlMission()) return;
        EnsureMissionDefaults();
        EnsureOwner();

        _missionIndex++;
        if (missionTitles != null && _missionIndex >= missionTitles.Length)
        {
            _missionIndex = 0;
        }

        _missionPhase = 0;
        _attempts = 0;
        _lastGrade = 0;

        RequestSerialization();
        ResetExperimentLocal();
        RefreshAllDisplaysLocal();
    }

    public void _MissionHint()
    {
        // ヒントは“非同期表示”でOK（同期しなくて良い）
        if (spawner == null) return;

        string hint = BuildHintLocal();
        // 既存UIテキスト（spawner側）を使う。未設定なら何もしない。
        if (spawner.hintText != null) spawner.hintText.text = hint;

        RefreshAllDisplaysLocal();
    }

    public void _MissionSubmit()
    {
        // 手動採点（実験完了前でも採点できる）
        if (!CanControlMission()) return;
        GradeAndSync();
    }

    // Called by ChemElementSpawner (local event on each client)
    public void _OnExperimentCompleted()
    {
        if (!autoGradeOnComplete) return;
        if (!CanControlMission()) return;
        if (_missionPhase != 1) return; // running only

        GradeAndSync();
    }

    // Called by spawner when phase changes; good for UI refresh
    public void _OnSpawnerPhaseChanged()
    {
        // UI refresh only (local)
        RefreshAllDisplaysLocal();
    }

    public override void OnDeserialization()
    {
        // mission state changed from remote
        if (_lastSeenMissionIndex != _missionIndex || _lastSeenMissionPhase != _missionPhase)
        {
            _lastSeenMissionIndex = _missionIndex;
            _lastSeenMissionPhase = _missionPhase;
            RefreshAllDisplaysLocal();
        }
    
        ApplyPresentationMissionLock();
}

    // ============ Internals ============
    private void EnsureOwner()
    {
        VRCPlayerApi lp = Networking.LocalPlayer;
        if (lp == null) return;
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(lp, gameObject);
        }
    }

    private bool CanControlMission()
    {
        if (spawner == null) return false;
        // 操作者のみミッションを動かす（同期の一貫性）
        return spawner.IsOperatorLocal();
    }

    private void ResetExperimentLocal()
    {
        if (spawner != null) spawner.SendCustomEvent("_ResetExperiment");
        if (environmentManager != null) environmentManager.SendCustomEvent("_ResetToDefaults");
    }

    private void RefreshAllDisplaysLocal()
    {
        if (uiSync != null) uiSync.SendCustomEvent("_RefreshAllDisplays");
    }

    private string NormalizeFormula(string s)
    {
        if (s == null) return "";
        // remove spaces and unify case
        s = s.Replace(" ", "");
        s = s.Replace("\n", "");
        s = s.Replace("\r", "");
        return s.ToUpper();
    }

    private void GradeAndSync()
    {
        EnsureMissionDefaults();
        EnsureOwner();

        _attempts++;

        string expected = GetMissionGoal();
        string actual = spawner != null ? spawner.GetProductFormula() : "";
        if (string.IsNullOrEmpty(actual))
        {
            // 実験が完了していない場合は表示式で代用
            actual = spawner != null ? spawner.GetDisplayFormulaForUI() : "";
        }

        int pts = 1;
        if (missionPoints != null && _missionIndex >= 0 && _missionIndex < missionPoints.Length)
        {
            pts = missionPoints[_missionIndex];
            if (pts <= 0) pts = 1;
        }

        // ---------- Conditions ----------
        bool okProduct = true;
        if (!string.IsNullOrEmpty(expected))
        {
            okProduct = NormalizeFormula(actual) == NormalizeFormula(expected);
        }

        // Tool requirement
        string requiredTool = GetMissionRequiredToolId();
        string actualTool = spawner != null ? spawner.GetLastEquipment() : "";
        bool okTool = true;
        if (!string.IsNullOrEmpty(requiredTool))
        {
            okTool = NormalizeFormula(actualTool) == NormalizeFormula(requiredTool);
        }

        // Temp range requirement (submit-time synced temp)
        float t = spawner != null ? spawner.GetSyncedTemperatureC() : 0f;
        float tMin = GetMissionMinTempC();
        float tMax = GetMissionMaxTempC();
        bool okTemp = true;
        if (tMax > tMin)
        {
            okTemp = (t >= tMin && t <= tMax);
        }

        // Operation requirements (max reached during run)
        float maxHeat = spawner != null ? spawner.GetMaxHeat01() : 0f;
        float maxStir = spawner != null ? spawner.GetMaxStir01() : 0f;
        float maxPour = spawner != null ? spawner.GetMaxPour01() : 0f;
        float maxShake = spawner != null ? spawner.GetMaxShake01() : 0f;

        float reqHeat = GetMissionMinHeat01();
        float reqStir = GetMissionMinStir01();
        float reqPour = GetMissionMinPour01();
        float reqShake = GetMissionMinShake01();

        bool okHeat = reqHeat <= 0f || maxHeat >= reqHeat;
        bool okStir = reqStir <= 0f || maxStir >= reqStir;
        bool okPour = reqPour <= 0f || maxPour >= reqPour;
        bool okShake = reqShake <= 0f || maxShake >= reqShake;

        bool okComplete = true;
        if (GetMissionRequireComplete())
        {
            okComplete = spawner != null && spawner.GetPhase() == 2;
        }

        bool ok = okProduct && okTool && okTemp && okHeat && okStir && okPour && okShake && okComplete;
        if (ok)
        {
            _lastGrade = 1;
            _score += pts;
        }
        else
        {
            _lastGrade = -1;
        }

        _missionPhase = 2; // graded
        RequestSerialization();

        // Local feedback
        if (spawner != null && spawner.explainText != null)
        {
            spawner.explainText.text = BuildFeedbackLocal(ok, expected, actual, pts,
                requiredTool, actualTool, t, tMin, tMax,
                reqHeat, maxHeat, reqStir, maxStir, reqPour, maxPour, reqShake, maxShake,
                okComplete);
        }

        if (autoAdvanceOnSuccess && ok)
        {
            // 次ミッションへ（同期）
            _missionPhase = 0;
            _attempts = 0;
            RequestSerialization();
            _MissionNext();
            return;
        }

        RefreshAllDisplaysLocal();
    }

    private string BuildHintLocal()
    {
        string title = GetMissionTitle();
        string goal = GetMissionGoal();
        string prompt = GetMissionPrompt();
        string input = spawner != null ? spawner.GetInputFormula() : "";
        int phase = spawner != null ? spawner.GetPhase() : 0;

        string s = "";
        s += "【ヒント】\n";
        if (!string.IsNullOrEmpty(title)) s += "Mission: " + title + "\n";
        if (!string.IsNullOrEmpty(goal)) s += "Goal: " + goal + "\n";
        if (!string.IsNullOrEmpty(prompt)) s += prompt + "\n\n";

        if (string.IsNullOrEmpty(input))
        {
            s += "まず元素を選んで投入してみよう。\n";
        }
        else
        {
            s += "現在の入力: " + input + "\n";
        }

        if (phase == 0) s += "実験を開始して反応を進めよう。\n";
        if (phase == 1) s += "反応が進行中。必要なら加熱/攪拌を調整しよう。\n";
        if (phase == 2) s += "反応が完了。生成物を確認して提出しよう。\n";

        return s;
    }

    private string BuildFeedbackLocal(
        bool ok,
        string expected,
        string actual,
        int pts,
        string requiredTool,
        string actualTool,
        float tempC,
        float tempMinC,
        float tempMaxC,
        float reqHeat,
        float maxHeat,
        float reqStir,
        float maxStir,
        float reqPour,
        float maxPour,
        float reqShake,
        float maxShake,
        bool okComplete
    )
    {
        string s = "";
        s += ok ? "✅ PASS\n" : "❌ FAIL\n";

        s += "\n--- Result ---\n";
        if (!string.IsNullOrEmpty(expected)) s += "GoalProduct: " + expected + "\n";
        s += "YourProduct: " + actual + "\n";

        s += "\n--- Checks ---\n";
        if (!string.IsNullOrEmpty(expected))
        {
            bool okP = NormalizeFormula(actual) == NormalizeFormula(expected);
            s += (okP ? "✅ " : "❌ ") + "Product match\n";
        }

        if (!string.IsNullOrEmpty(requiredTool))
        {
            bool okT = NormalizeFormula(actualTool) == NormalizeFormula(requiredTool);
            s += (okT ? "✅ " : "❌ ") + "Tool: need " + requiredTool + " / used " + actualTool + "\n";
        }
        else
        {
            s += "(Tool: any)\n";
        }

        if (tempMaxC > tempMinC)
        {
            bool okTemp = (tempC >= tempMinC && tempC <= tempMaxC);
            s += (okTemp ? "✅ " : "❌ ") + "Temp: " + tempC.ToString("0.0") + "C in [" + tempMinC.ToString("0.0") + ", " + tempMaxC.ToString("0.0") + "]\n";
        }
        else
        {
            s += "(Temp: any)\n";
        }

        if (reqHeat > 0f) s += (maxHeat >= reqHeat ? "✅ " : "❌ ") + "Heat max " + maxHeat.ToString("0.00") + " >= " + reqHeat.ToString("0.00") + "\n";
        if (reqStir > 0f) s += (maxStir >= reqStir ? "✅ " : "❌ ") + "Stir max " + maxStir.ToString("0.00") + " >= " + reqStir.ToString("0.00") + "\n";
        if (reqPour > 0f) s += (maxPour >= reqPour ? "✅ " : "❌ ") + "Pour max " + maxPour.ToString("0.00") + " >= " + reqPour.ToString("0.00") + "\n";
        if (reqShake > 0f) s += (maxShake >= reqShake ? "✅ " : "❌ ") + "Shake max " + maxShake.ToString("0.00") + " >= " + reqShake.ToString("0.00") + "\n";
        if (reqHeat <= 0f && reqStir <= 0f && reqPour <= 0f && reqShake <= 0f) s += "(Operation: any)\n";

        if (GetMissionRequireComplete())
        {
            s += (okComplete ? "✅ " : "❌ ") + "Require Complete (phase=2)\n";
        }

        s += "\nPoints: " + (ok ? ("+" + pts) : "+0") + "\n";
        s += "Score: " + _score + "\n";
        s += "Attempts: " + _attempts + "\n";

        s += "\n--- Reflection ---\n";
        if (ok)
        {
            s += "条件（器具/温度/操作）が結果にどう影響したか説明してみよう。\n";
        }
        else
        {
            s += "失敗したチェック項目を満たすように、器具/温度/攪拌などを調整して再挑戦しよう。\n";
        }
        return s;
    }

    private void EnsureMissionDefaults()
    {
        // If not configured, provide a small default set (can be overwritten by Importer later)
        if (missionTitles != null && missionTitles.Length > 0 &&
            missionGoalProductFormula != null && missionGoalProductFormula.Length > 0) return;

        missionTitles = new string[]
        {
            "Water Synthesis",
            "Salt Formation",
            "Carbon Dioxide Generation",
            "Ammonia Synthesis"
        };

        missionPrompts = new string[]
        {
            "水(H2O)を作ろう。入力元素と条件を整えて、生成物を確認して提出。",
            "食塩(NaCl)を作ろう。目的の生成物になるように組み合わせを考える。",
            "二酸化炭素(CO2)を作ろう。どの元素/条件で発生する？",
            "アンモニア(NH3)を作ろう。温度や条件で結果が変わることに注目。"
        };

        missionGoalProductFormula = new string[]
        {
            "H2O",
            "NaCl",
            "CO2",
            "NH3"
        };

        missionPoints = new int[] { 2, 2, 3, 4 };

        // --- Conditions defaults (example) ---
        // Tool IDs are examples. Replace via Importer.
        missionRequiredToolId = new string[]
        {
            "beaker",   // water
            "beaker",   // salt
            "beaker",   // CO2
            "reactor"   // NH3
        };

        // Temp range: max<=min means "ignore".
        missionMinTempC = new float[] { 0f, 20f, 15f, 350f };
        missionMaxTempC = new float[] { 100f, 80f, 60f, 550f };

        // Operation: require reaching these levels at least once during run.
        missionMinHeat01 = new float[] { 0.2f, 0f, 0f, 0.6f };
        missionMinStir01 = new float[] { 0.3f, 0.2f, 0.1f, 0.4f };
        missionMinPour01 = new float[] { 0.1f, 0.2f, 0.2f, 0f };
        missionMinShake01 = new float[] { 0f, 0f, 0f, 0f };

        missionRequireComplete = new int[] { 1, 1, 1, 1 };
    }


    private void ApplyPresentationMissionLock()
    {
        if (!forceSingleMission) return;

        EnsureMissionDefaults();

        // Optionally overwrite mission 0 so even if the inspector arrays are custom, presentation is stable.
        if (overwriteMissionZeroForPresentation)
        {
            if (missionTitles != null && missionTitles.Length > 0) missionTitles[0] = "Hydrogen + Chlorine (Photo Explosion)";
            if (missionPrompts != null && missionPrompts.Length > 0) missionPrompts[0] = "Mix H2 and Cl2, then trigger light to form HCl.";
            if (missionGoalProductFormula != null && missionGoalProductFormula.Length > 0) missionGoalProductFormula[0] = forcedMissionGoalProductFormula;
            if (missionRequiredToolId != null && missionRequiredToolId.Length > 0) missionRequiredToolId[0] = ""; // keep unblocked
            if (missionMinTempC != null && missionMinTempC.Length > 0) missionMinTempC[0] = -273f;
            if (missionMaxTempC != null && missionMaxTempC.Length > 0) missionMaxTempC[0] = 9999f;
        }

        // Lock mission index to 0
        if (Networking.IsOwner(gameObject))
        {
            _missionIndex = 0;
            _missionPhase = 0;
            RequestSerialization();
        }
        else
        {
            EnsureOwner();
            _missionIndex = 0;
            _missionPhase = 0;
            RequestSerialization();
        }
    }

}
