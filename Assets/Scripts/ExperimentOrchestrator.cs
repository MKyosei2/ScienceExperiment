using UnityEngine;
using System.Collections;

public class ExperimentOrchestrator : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private SelectedObjectHolder selected;
    [SerializeField] private GenericSelector[] autoSpawnSelectors;

    [Header("I/O & Effects")]
    [SerializeField] private VRExperimentMonitor monitor;
    [SerializeField] private VisualExperimentPlayer visualPlayer;
    [SerializeField] private JsonReactionPlayer jsonPlayer;
    [SerializeField] private AIRequestSender ai;

    [Header("Options")]
    [SerializeField] private float fallbackDelay = 2f;
    [SerializeField] private bool playVisualOnStart = true;

    public void StartExperiment()
    {
        StartCoroutine(CoRun());
    }

    private IEnumerator CoRun()
    {
        monitor?.Log("Experiment: Start requested.");

        foreach (var s in autoSpawnSelectors)
        {
            if (s && s.CurrentPrefab && s.TargetZone && s.TargetZone.childCount == 0)
            {
                var go = s.SpawnOrReplace();
                monitor?.Log($"Spawned: {go?.name}");
            }
        }

        if (!selected || !selected.IsValid())
        {
            monitor?.LogWarning("Selection invalid. Aborting.");
            yield break;
        }

        if (playVisualOnStart) visualPlayer?.PlayStart(selected);

        bool finished = false;
        ai.Send(selected, onSuccess: response =>
        {
            finished = true;
            monitor?.Log("AI response received.");
            if (!string.IsNullOrEmpty(response))
            {
                if (JsonHelper.IsJson(response))
                    jsonPlayer?.Play(response);
                else
                    visualPlayer?.PlayMessage(response);
            }
        },
        onError: err =>
        {
            finished = true;
            monitor?.LogError($"AI error: {err}");
            visualPlayer?.PlayFallback();
        });

        float t = 0f;
        while (!finished && t < fallbackDelay) { t += Time.deltaTime; yield return null; }

        if (!finished)
        {
            monitor?.LogWarning("AI no response. Playing fallback.");
            visualPlayer?.PlayFallback();
        }

        monitor?.Log("Experiment: End.");
    }
}
