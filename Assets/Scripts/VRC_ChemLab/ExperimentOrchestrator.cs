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

    [Header("Mission Data (can be overwritten via Importer)")]
    public string[] missionTitles;
    [TextArea] public string[] missionPrompts;
    public string[] missionGoalProductFormula; // 例: "H2O", "NaCl"
    public int[] missionPoints;

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

        bool ok = NormalizeFormula(actual) == NormalizeFormula(expected);
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
            spawner.explainText.text = BuildFeedbackLocal(ok, expected, actual, pts);
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

    private string BuildFeedbackLocal(bool ok, string expected, string actual, int pts)
    {
        string s = "";
        s += ok ? "✅ 正解！\n" : "❌ 不正解。\n";
        s += "Expected: " + expected + "\n";
        s += "Actual: " + actual + "\n";
        if (ok) s += "Points: +" + pts + "\n";
        s += "Score: " + _score + "\n";
        s += "Attempts: " + _attempts + "\n";
        s += "\n振り返り:\n";
        if (ok)
        {
            s += "生成物が目的どおりになった理由を、温度/器具/入力の観点で説明してみよう。\n";
        }
        else
        {
            s += "入力元素、器具、温度、攪拌などの条件を変えて再挑戦してみよう。\n";
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
    }
}
