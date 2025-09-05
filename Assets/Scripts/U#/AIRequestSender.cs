using UdonSharp;
using UnityEngine;

public class AIRequestSender : UdonSharpBehaviour
{
    [Header("Wiring")]
    public ExperimentOrchestrator orchestrator;

    [Header("Mock Settings")]
    public bool returnJson = true;
    public float responseDelay = 1.0f;
    [TextArea] public string mockJson = "{\"effects\":[{\"type\":\"bubble\",\"color\":\"blue\",\"intensity\":0.7}]}";
    [TextArea] public string mockText = "うん、反応が起きたみたい！液体が青くなっているよ。";

    [HideInInspector] public string lastResponse;
    [HideInInspector] public string lastError;

    private bool _pending;
    private float _timer;

    public void Request(SelectedObjectHolder selected)
    {
        _pending = true;
        _timer = 0f;
        lastResponse = null;
        lastError = null;
    }

    private void Update()
    {
        if (!_pending) return;
        _timer += Time.deltaTime;
        if (_timer >= responseDelay)
        {
            _pending = false;
            lastResponse = returnJson ? mockJson : mockText;
            if (orchestrator != null) orchestrator.SendCustomEvent("OnAIResponse");
        }
    }
}
