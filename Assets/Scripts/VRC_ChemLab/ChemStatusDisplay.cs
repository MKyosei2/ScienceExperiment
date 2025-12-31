using UdonSharp;
using UnityEngine;
using TMPro;

/// <summary>
/// ChemStatusDisplay
/// - UI表示は非同期（各クライアントで更新）
/// - 実験の真実（phase/temp etc）は ChemElementSpawner の同期値から取得
/// </summary>
public class ChemStatusDisplay : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager env;
    public ExperimentOrchestrator orchestrator;
    public TextMeshProUGUI statusText;

    [Header("Auto Refresh (optional)")]
    public bool autoRefresh = true;
    [Tooltip("自動更新の間隔（秒）。0以下で無効")]
    public float refreshInterval = 0.25f;

    private float _nextRefresh;

    private void Start()
    {
        if (autoRefresh && refreshInterval > 0f)
        {
            _nextRefresh = Time.time + 0.1f;
        }
        RefreshUI();
    }

    private void Update()
    {
        if (!autoRefresh) return;
        if (refreshInterval <= 0f) return;
        if (Time.time < _nextRefresh) return;
        _nextRefresh = Time.time + refreshInterval;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (statusText == null) return;

        string e = spawner != null ? spawner.GetInputFormula() : "";
        string display = spawner != null ? spawner.GetDisplayFormulaForUI() : "";
        string product = spawner != null ? spawner.GetProductFormula() : "";
        string t = spawner != null ? spawner.GetLastEquipment() : "";
        string logs = spawner != null ? spawner.GetHistoryLog() : "";

        int phase = spawner != null ? spawner.GetPhase() : 0;
        float prog = spawner != null ? spawner.GetProgress01() : 0f;
        float heat01 = spawner != null ? spawner.GetHeat01() : 0f;
        float tempSynced = spawner != null ? spawner.GetSyncedTemperatureC() : (env != null ? env.Temperature : 0f);
        float tempVisual = spawner != null ? spawner.GetCurrentTemperatureC() : tempSynced;
        float ambient = spawner != null ? spawner.GetAmbientTemperatureC() : (env != null ? env.Temperature : 0f);
        string tag = spawner != null ? spawner.GetReactionTag() : "none";

        float hum = env != null ? env.Humidity : 0f;
        float pres = env != null ? env.Pressure : 0f;

        string phaseLabel = (phase == 0) ? "Idle" : (phase == 1) ? "Running" : (phase == 2) ? "Complete" : phase.ToString();

        
        string missionTitle = orchestrator != null ? orchestrator.GetMissionTitle() : "";
        string missionGoal = orchestrator != null ? orchestrator.GetMissionGoal() : "";
        int missionIndex = orchestrator != null ? orchestrator.GetMissionIndex() : -1;
        int score = orchestrator != null ? orchestrator.GetScore() : 0;
        int attempts = orchestrator != null ? orchestrator.GetAttempts() : 0;
        string lastGrade = orchestrator != null ? orchestrator.GetLastGradeText() : "";
        string missionPhase = orchestrator != null ? orchestrator.GetMissionPhaseText() : "";

statusText.text =
            "--- Mission ---\n" +
            "Index: " + missionIndex + "\n" +
            "Title: " + missionTitle + "\n" +
            "Goal: " + missionGoal + "\n" +
            "MissionPhase: " + missionPhase + "\n" +
            "Score: " + score + "  Attempts: " + attempts + "\n" +
            "Last: " + lastGrade + "\n\n" +
            "--- Experiment Status ---\n" +
            "Phase: " + phaseLabel + " (" + Mathf.RoundToInt(prog * 100f) + "%)\n" +
            "ReactionTag: " + tag + "\n" +
            "Tool: " + t + "\n" +
            "Input: " + e + "\n" +
            "Display: " + display + "\n" +
            "Product: " + product + "\n\n" +
            "--- Environment ---\n" +
            "Temp(sync): " + tempSynced.ToString("0.0") + " °C\n" +
            "Temp(visual): " + tempVisual.ToString("0.0") + " °C\n" +
            "Ambient: " + ambient.ToString("0.0") + " °C\n" +
            "Heat01: " + heat01.ToString("0.00") + "\n" +
            "Humidity: " + hum.ToString("0.0") + " %\n" +
            "Pressure: " + pres.ToString("0.0") + " kPa\n\n" +
            "--- Logs ---\n" +
            logs;
    }
}
