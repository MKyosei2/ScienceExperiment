using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UdonSharp;
using VRC.Udon;
using VRC.SDKBase;
using System.Linq;

public class ChemLabUltimateInspectorSetup : EditorWindow
{
    [MenuItem("CHEMLAB/Link ALL Inspector References (Ultimate)")]
    public static void LinkEverything()
    {
        Link<ModeLabelUI>("ModeLabel", (comp, go) => comp.modeText = GetOrAdd<TextMeshProUGUI>(go));
        Link<StatusTextUI>("StatusText", (comp, go) => comp.statusText = GetOrAdd<TextMeshProUGUI>(go));
        Link<AIRequestSender>("AIRequestSender", (comp, go) => {
            comp.monitor = Find<VRExperimentMonitor>();
            comp.statusText = GetOrAdd<TextMeshProUGUI>(GameObject.Find("StatusText"));
        });
        Link<ExperimentStartButton>("ExperimentStartButton", (comp, go) => {
            comp.experimentController = GetUdon("ExperimentController");
            comp.statusTextUI = GetUdon("StatusText");
        });
        Link<ExperimentController>("ExperimentController", (comp, go) => {
            comp.holder = Find<SelectedObjectHolder>();
            comp.requestSender = Find<AIRequestSender>();
            comp.modeSwitcher = Find<ModeSwitcher>();
        });
        Link<SelectionSummaryDisplay>("SelectionSummaryDisplay", (comp, go) => {
            comp.holder = Find<SelectedObjectHolder>();
            comp.outputText = GetOrAdd<TextMeshProUGUI>(GameObject.Find("StatusText"));
        });
        Link<AIReactionHandler>("AIReactionHandler", (comp, go) => {
            comp.spawner = Find<CompoundSpawner>();
            comp.resultText = GetOrAdd<Text>(GameObject.Find("ResultText"));
            comp.audioSource = Find<AudioSource>();
        });
        Link<CompoundSpawner>("CompoundSpawner", (comp, go) => {
            comp.spawnPoint = GameObject.Find("ReactionSpawnPoint")?.transform;
        });
        Link<HistoryViewer>("HistoryViewer", (comp, go) => {
            comp.history = Find<ExperimentHistory>();
            comp.output = GetOrAdd<TextMeshProUGUI>(GameObject.Find("HistoryText"));
        });
        Link<ResultReceiver>("ResultReceiver", (comp, go) => {
            comp.resultText = GetOrAdd<TextMeshProUGUI>(GameObject.Find("ResultText"));
            comp.triviaText = GetOrAdd<TextMeshProUGUI>(GameObject.Find("TriviaText"));
            comp.history = Find<ExperimentHistory>();
            comp.holder = Find<SelectedObjectHolder>();
        });
        Link<ExperimentExecutor>("ExperimentExecutor", (comp, go) => {
            comp.holder = Find<SelectedObjectHolder>();
            comp.spawnPoint = GameObject.Find("ElementExperimentZone")?.transform;
        });
        Link<ZoneSelectionButton>("ZoneSelectionButton", (comp, go) => {
            comp.selectionZone = Find<SelectionZone>();
            comp.holder = Find<SelectedObjectHolder>();
        });
        Link<ModeSwitchButton>("ModeSwitchButton", (comp, go) => {
            comp.modeSwitcher = Find<ModeSwitcher>();
        });
        Link<ObjectSpawnerButton>("ObjectSpawnerButton", (comp, go) => {
            comp.holder = Find<SelectedObjectHolder>();
            comp.modeSwitcher = Find<ModeSwitcher>();
            comp.spawnPoint = GameObject.Find("ElementExperimentZone")?.transform;
        });

        Debug.Log("✅ All known inspector references have been linked.");
    }

    static void Link<T>(string name, System.Action<T, GameObject> assign) where T : Component
    {
        var comp = Find<T>();
        if (comp == null) return;
        var go = comp.gameObject;
        Undo.RecordObject(comp, "Auto Link Fields");
        assign.Invoke(comp, go);
        EditorUtility.SetDirty(comp);
    }

    static T Find<T>() where T : Object => GameObject.FindObjectOfType<T>();
    static T GetOrAdd<T>(GameObject go) where T : Component => go ? (go.GetComponent<T>() ?? go.AddComponent<T>()) : null;
    static UdonBehaviour GetUdon(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<UdonBehaviour>() : null;
    }
}
