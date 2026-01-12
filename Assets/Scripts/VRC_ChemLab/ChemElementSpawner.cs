
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;

/// <summary>
/// ChemElementSpawner (Compatibility + Force Visible)
/// - Keeps the public API that other scripts expect.
/// - Ensures CONICAL_FLASK is spawned/visible when an element is selected.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ChemElementSpawner : UdonSharpBehaviour
{
    // ===== References expected around the project =====
    [Header("Managers (optional)")]
    public ChemEnvironmentManager environmentManager;

    [Header("UI (optional)")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI explainText;

    [Header("Visual (optional)")]
    public ChemVisualController sampleVisual;
    public ChemReactionAnimator reactionAnimator;

    // ===== Auto Spawn Visual Container =====
    [Header("Auto Spawn Visual Container")]
    [Tooltip("Drag Assets/Prefabs/CONICAL_FLASK prefab here")]
    public GameObject conicalFlaskPrefab;

    [Tooltip("Drag ExperimentTable上のZone here (optional; auto-find by name if null)")]
    public Transform experimentZone;

    [Tooltip("Local offset from Zone")]
    public Vector3 flaskLocalOffset = Vector3.zero;

    private GameObject _spawnedFlask;

    // ===== Internal state (kept minimal but compatible) =====
    [UdonSynced] private float _heat01;
    [UdonSynced] private float _stir01;
    [UdonSynced] private float _pour01;
    [UdonSynced] private float _shake01;

    [UdonSynced] private float _maxHeat01;
    [UdonSynced] private float _maxStir01;
    [UdonSynced] private float _maxPour01;
    [UdonSynced] private float _maxShake01;

    [UdonSynced] private float _syncedTempC;
    [UdonSynced] private float _minTempReachedC;
    [UdonSynced] private float _maxTempReachedC;

    [UdonSynced] private int _phase; // 0:idle, 1:running, 2:done etc (project dependent)
    [UdonSynced] private float _progress01;
    [UdonSynced] private string _reactionTag = "none";

    [UdonSynced] private string _inputFormula = "";
    [UdonSynced] private string _displayFormulaUI = "";
    [UdonSynced] private string _productFormula = "";
    [UdonSynced] private string _lastEquipment = "";
    [UdonSynced] private string _historyLog = "";

    // ===== Public API expected by other scripts =====

    // Called by UI / buttons in some builds
    public void SpawnElement()
    {
        SelectElement();
    }

    // Selection (element/equipment) — project dependent; keep as entry points
    public void SelectElement()
    {
        EnsureConicalFlaskVisible();

        // If you have a visual controller, let it know "something changed"
        if (sampleVisual != null)
        {
            // optional: mark it as element mode without assuming internals
            // sampleVisual could be used by ChemStatusDisplay for local info
        }

        if (reactionAnimator != null)
        {
            // Keep compatibility with old calls; no-op stubs exist on animator
            reactionAnimator.ResetLevels();
        }
    }

    public void SelectEquipment()
    {
        // Keep method present for completeness; equipment button may call this indirectly
        EnsureConicalFlaskVisible();
    }

    // Environment adjuster calls this
    public void ModifyEnvironment(string command)
    {
        // Operator gate: keep permissive by default to avoid blocking user locally
        if (environmentManager != null) environmentManager.Modify(command);
    }

    // VR input bridge uses this
    public void SetOps01(float heat01, float stir01, float pour01, float shake01)
    {
        _heat01 = Mathf.Clamp01(heat01);
        _stir01 = Mathf.Clamp01(stir01);
        _pour01 = Mathf.Clamp01(pour01);
        _shake01 = Mathf.Clamp01(shake01);

        if (_heat01 > _maxHeat01) _maxHeat01 = _heat01;
        if (_stir01 > _maxStir01) _maxStir01 = _stir01;
        if (_pour01 > _maxPour01) _maxPour01 = _pour01;
        if (_shake01 > _maxShake01) _maxShake01 = _shake01;

        RequestSerialization();
    }

    public float GetHeat01() => _heat01;
    public float GetStir01() => _stir01;
    public float GetPour01() => _pour01;
    public float GetShake01() => _shake01;

    public float GetMaxHeat01() => _maxHeat01;
    public float GetMaxStir01() => _maxStir01;
    public float GetMaxPour01() => _maxPour01;
    public float GetMaxShake01() => _maxShake01;

    public int GetPhase() => _phase;
    public float GetProgress01() => _progress01;

    public float GetSyncedTemperatureC() => _syncedTempC;
    public float GetMinTempReachedC() => _minTempReachedC;
    public float GetMaxTempReachedC() => _maxTempReachedC;

    public float GetCurrentTemperatureC() => _syncedTempC;
    public float GetAmbientTemperatureC() => environmentManager != null ? environmentManager.Temperature : _syncedTempC;

    public string GetReactionTag() => _reactionTag;

    public string GetInputFormula() => _inputFormula;
    public string GetDisplayFormulaForUI() => _displayFormulaUI;
    public string GetProductFormula() => _productFormula;
    public string GetLastEquipment() => _lastEquipment;
    public string GetHistoryLog() => _historyLog;

    public bool HasOperator() => true;
    public bool IsOperatorLocal() => true;

    // Called directly by StartExperimentButton in this project
    public void _StartExperiment()
    {
        // Ensure container exists so the user immediately sees something.
        EnsureConicalFlaskVisible();

        _phase = 1;
        _progress01 = 0f;
        _historyLog = AppendLog(_historyLog, "StartExperiment");
        RequestSerialization();
    }

    // Used by OperatorButton via SendCustomEvent
    public void _ReleaseOperator()
    {
        _historyLog = AppendLog(_historyLog, "ReleaseOperator");
        RequestSerialization();
    }

    // ===== Visual spawn implementation =====
    private void EnsureConicalFlaskVisible()
    {
        if (_spawnedFlask != null) return;

        // Auto-find Zone if not assigned
        if (experimentZone == null)
        {
            // common names
            var z = GameObject.Find("Zone");
            if (z == null) z = GameObject.Find("VR_StartZone");
            if (z == null)
            {
                // try deep path-ish
                var expTable = GameObject.Find("ExperimentTable");
                if (expTable != null)
                {
                    var t = expTable.transform.Find("Zone");
                    if (t != null) z = t.gameObject;
                    else
                    {
                        t = expTable.transform.Find("VR_StartZone");
                        if (t != null) z = t.gameObject;
                    }
                }
            }
            if (z != null) experimentZone = z.transform;
        }

        if (conicalFlaskPrefab == null)
        {
            Debug.LogError("[ChemElementSpawner] conicalFlaskPrefab is not assigned. Drag Assets/Prefabs/CONICAL_FLASK into inspector.");
            return;
        }

        Vector3 pos = experimentZone != null ? experimentZone.position : transform.position;
        Quaternion rot = experimentZone != null ? experimentZone.rotation : Quaternion.identity;
        pos += (experimentZone != null ? experimentZone.TransformVector(flaskLocalOffset) : flaskLocalOffset);

        _spawnedFlask = VRCInstantiate(conicalFlaskPrefab);
        _spawnedFlask.transform.SetPositionAndRotation(pos, rot);
        _spawnedFlask.SetActive(true);

        // Force visible even if prefab has disabled renderers/particles
        var renderers = _spawnedFlask.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
            renderers[i].gameObject.layer = 0; // Default
        }

        var particles = _spawnedFlask.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].gameObject.SetActive(true);
            particles[i].Play(true);
        }

        // If we have sampleVisual anchor concept, optionally parent it under flask
        if (sampleVisual != null)
        {
            // Keep existing sampleVisual in scene, just move it near flask so UI reflects it
            sampleVisual.transform.position = _spawnedFlask.transform.position;
        }

        _historyLog = AppendLog(_historyLog, "Spawn CONICAL_FLASK");
    }

    private string AppendLog(string log, string line)
    {
        if (string.IsNullOrEmpty(log)) return line;
        return log + "\n" + line;
    }
}
