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

    [Header("Visual Recipe (optional)")]
    public ChemVisualController visual;

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
        if (visual == null && spawner != null) visual = spawner.sampleVisual;
        RefreshUI();
    }

    private void Update()
    {
        if (!autoRefresh) return;
        if (refreshInterval <= 0f) return;
        if (Time.time < _nextRefresh) return;
        _nextRefresh = Time.time + refreshInterval;
        if (visual == null && spawner != null) visual = spawner.sampleVisual;
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
        float stir01 = spawner != null ? spawner.GetStir01() : 0f;
        float pour01 = spawner != null ? spawner.GetPour01() : 0f;
        float shake01 = spawner != null ? spawner.GetShake01() : 0f;

        float maxHeat = spawner != null ? spawner.GetMaxHeat01() : 0f;
        float maxStir = spawner != null ? spawner.GetMaxStir01() : 0f;
        float maxPour = spawner != null ? spawner.GetMaxPour01() : 0f;
        float maxShake = spawner != null ? spawner.GetMaxShake01() : 0f;

        float tempSynced = spawner != null ? spawner.GetSyncedTemperatureC() : (env != null ? env.Temperature : 0f);
        float tMinReached = spawner != null ? spawner.GetMinTempReachedC() : tempSynced;
        float tMaxReached = spawner != null ? spawner.GetMaxTempReachedC() : tempSynced;
        float tempVisual = spawner != null ? spawner.GetCurrentTemperatureC() : tempSynced;
        float ambient = spawner != null ? spawner.GetAmbientTemperatureC() : (env != null ? env.Temperature : 0f);
        string tag = spawner != null ? spawner.GetReactionTag() : "none";

        // Visual recipe info (local)
        if (visual == null && spawner != null) visual = spawner.sampleVisual;
        string recipeSource = visual != null ? (visual.lastRecipeSource ?? "") : "";
        string recipeType = visual != null ? (visual.lastIsKnownCompound ? "KnownCompound" : (visual.lastIsElement ? "Element" : "Inferred")) : "";
        string arch = visual != null ? ArchetypeToLabel(visual.lastArchetype) : "";
        string preset = visual != null ? PresetToLabel(visual.lastParticlePreset) : "";
        string infer = visual != null ? (visual.lastInferenceNote ?? "") : "";

        float hum = env != null ? env.Humidity : 0f;
        float pres = env != null ? env.Pressure : 0f;

        string phaseLabel = (phase == 0) ? "Idle" : (phase == 1) ? "Running" : (phase == 2) ? "Complete" : phase.ToString();

        
        string missionTitle = orchestrator != null ? orchestrator.GetMissionTitle() : "";
        string missionGoal = orchestrator != null ? orchestrator.GetMissionGoal() : "";
        string reqTool = orchestrator != null ? orchestrator.GetMissionRequiredToolId() : "";
        float reqTempMin = orchestrator != null ? orchestrator.GetMissionMinTempC() : 0f;
        float reqTempMax = orchestrator != null ? orchestrator.GetMissionMaxTempC() : 0f;
        float reqHeat = orchestrator != null ? orchestrator.GetMissionMinHeat01() : 0f;
        float reqStir = orchestrator != null ? orchestrator.GetMissionMinStir01() : 0f;
        float reqPour = orchestrator != null ? orchestrator.GetMissionMinPour01() : 0f;
        float reqShake = orchestrator != null ? orchestrator.GetMissionMinShake01() : 0f;
        bool reqComplete = orchestrator != null ? orchestrator.GetMissionRequireComplete() : false;
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
            "NeedTool: " + (string.IsNullOrEmpty(reqTool) ? "(any)" : reqTool) + "\n" +
            "NeedTemp: " + ((reqTempMax > reqTempMin) ? (reqTempMin.ToString("0.0") + ".." + reqTempMax.ToString("0.0") + "C") : "(any)") + "\n" +
            "NeedOps: " +
                (reqHeat > 0f ? ("Heat>=" + reqHeat.ToString("0.00") + " ") : "") +
                (reqStir > 0f ? ("Stir>=" + reqStir.ToString("0.00") + " ") : "") +
                (reqPour > 0f ? ("Pour>=" + reqPour.ToString("0.00") + " ") : "") +
                (reqShake > 0f ? ("Shake>=" + reqShake.ToString("0.00") + " ") : "") +
                ((reqHeat <= 0f && reqStir <= 0f && reqPour <= 0f && reqShake <= 0f) ? "(any)" : "") +
                "\n" +
            "NeedComplete: " + (reqComplete ? "YES" : "NO") + "\n" +
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
            "Stir01: " + stir01.ToString("0.00") + "  Pour01: " + pour01.ToString("0.00") + "  Shake01: " + shake01.ToString("0.00") + "\n" +
            "MaxOps: Heat " + maxHeat.ToString("0.00") + " Stir " + maxStir.ToString("0.00") + " Pour " + maxPour.ToString("0.00") + " Shake " + maxShake.ToString("0.00") + "\n" +
            "TempReached: " + tMinReached.ToString("0.0") + ".." + tMaxReached.ToString("0.0") + " °C\n" +
            "Humidity: " + hum.ToString("0.0") + " %\n" +
            "Pressure: " + pres.ToString("0.0") + " kPa\n\n" +
            "--- Logs ---\n" +
            logs;
    }

    private string ArchetypeToLabel(int a)
    {
        if (a == ChemVisualController.ARCH_CRYSTAL) return "Crystal";
        if (a == ChemVisualController.ARCH_POWDER) return "Powder";
        if (a == ChemVisualController.ARCH_METAL) return "Metal";
        if (a == ChemVisualController.ARCH_LIQUID) return "Liquid";
        if (a == ChemVisualController.ARCH_GASFOG) return "Gas/Fog";
        return a.ToString();
    }

    private string PresetToLabel(int p)
    {
        if (p == ChemVisualController.PT_NONE) return "None";
        if (p == ChemVisualController.PT_GLINT) return "Glint";
        if (p == ChemVisualController.PT_PRECIPITATE) return "Precipitate";
        if (p == ChemVisualController.PT_BUBBLE) return "Bubble";
        if (p == ChemVisualController.PT_FOG) return "Fog";
        return p.ToString();
    }

}
