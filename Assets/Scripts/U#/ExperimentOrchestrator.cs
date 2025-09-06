using UdonSharp;
using UnityEngine;

public class ExperimentOrchestrator : UdonSharpBehaviour
{
    [Header("Selection")]
    public SelectedObjectHolder selected;

    // 旧: GenericSelector[] autoSpawnSelectors
    [Header("Auto Spawn (optional)")]
    public SpawnSelectorButton[] autoSpawnButtons; // ゾーンが空なら Press() して自動スポーン

    [Header("I/O & Effects")]
    public VRExperimentMonitor monitor;
    public VisualExperimentPlayer visualPlayer;
    public JsonReactionPlayer jsonPlayer;
    public AIRequestSender ai;

    [Header("Options")]
    public float fallbackDelay = 2f;
    public bool playVisualOnStart = true;

    private bool _waiting;
    private float _timer;

    public void StartExperiment()
    {
        if (monitor != null) monitor.Log("Experiment: Start requested.");

        // 自動スポーン（各ゾーンが空なら押す）
        if (autoSpawnButtons != null)
        {
            for (int i = 0; i < autoSpawnButtons.Length; i++)
            {
                SpawnSelectorButton b = autoSpawnButtons[i];
                if (b != null && b.zone != null)
                {
                    if (b.zone.childCount == 0) b.Press();
                }
            }
        }

        if (selected == null || !selected.IsValid())
        {
            if (monitor != null) monitor.LogWarning("Selection invalid. Aborting.");
            return;
        }

        if (playVisualOnStart && visualPlayer != null) visualPlayer.PlayStart(selected);

        if (ai != null)
        {
            _waiting = true;
            _timer = 0f;

            ai.orchestrator = this;
            ai.Request(selected);
        }
        else
        {
            if (monitor != null) monitor.LogWarning("AI sender missing. Fallback.");
            if (visualPlayer != null) visualPlayer.PlayFallback();
        }
    }

    private void Update()
    {
        if (_waiting)
        {
            _timer += Time.deltaTime;
            if (_timer >= fallbackDelay)
            {
                _waiting = false;
                if (monitor != null) monitor.LogWarning("AI no response. Playing fallback.");
                if (visualPlayer != null) visualPlayer.PlayFallback();
            }
        }
    }

    // === AIRequestSender からの通知 ===
    public void OnAIResponse()
    {
        _waiting = false;

        string resp = null;
        if (ai != null) resp = ai.lastResponse;

        if (monitor != null) monitor.Log("AI response received.");

        if (!string.IsNullOrEmpty(resp))
        {
            string s = resp.TrimStart();
            bool isJson = s.StartsWith("{") || s.StartsWith("[");
            if (isJson)
            {
                if (jsonPlayer != null) jsonPlayer.Play(resp);
            }
            else
            {
                if (visualPlayer != null) visualPlayer.PlayMessage(resp);
            }
        }
        else
        {
            if (visualPlayer != null) visualPlayer.PlayFallback();
        }
    }

    public void OnAIError()
    {
        _waiting = false;

        string err = "(unknown)";
        if (ai != null) err = ai.lastError;

        if (monitor != null) monitor.LogError("AI error: " + err);
        if (visualPlayer != null) visualPlayer.PlayFallback();
    }
}
