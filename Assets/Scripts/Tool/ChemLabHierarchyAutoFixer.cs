using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChemLabHierarchyAutoFixer
{
    [MenuItem("CHEMLAB VR/Validate & Auto-Fix Hierarchy")]
    public static void ValidateAndFix()
    {
        GameObject root = GameObject.Find("CHEMLAB_VR") ?? new GameObject("CHEMLAB_VR");

        // ---- GameSystems ----
        GameObject gameSystems = EnsureChild(root, "GameSystems");
        var modeSwitcher = EnsureComponent<ModeSwitcher>(gameSystems, "ModeSwitcher");
        var holder = EnsureComponent<SelectedObjectHolder>(gameSystems, "SelectedObjectHolder");
        var controller = EnsureComponent<ExperimentController>(gameSystems, "ExperimentController");
        var sender = EnsureComponent<AIRequestSender>(gameSystems, "AIRequestSender");
        var handler = EnsureComponent<AIReactionHandler>(gameSystems, "AIReactionHandler");
        var receiver = EnsureComponent<ResultReceiver>(gameSystems, "ResultReceiver");
        var history = EnsureComponent<ExperimentHistory>(gameSystems, "ExperimentHistory");

        controller.requestSender = sender;
        controller.holder = holder;
        sender.handler = handler;
        receiver.history = history;
        receiver.holder = holder;
        handler.spawner = null;
        handler.resultText = null;
        handler.audioSource = null;

        // ---- HistorySystem ----
        GameObject historySystem = EnsureChild(root, "HistorySystem");
        var viewer = EnsureComponent<HistoryViewer>(historySystem, "HistoryViewer");
        viewer.history = history;

        // ---- ExperimentObjects ----
        GameObject expObjects = EnsureChild(root, "ExperimentObjects");
        var spawner = EnsureComponent<CompoundSpawner>(expObjects, "CompoundSpawner");
        var executor = EnsureComponent<ExperimentExecutor>(expObjects, "ExperimentExecutor");
        handler.spawner = spawner;

        GameObject startBtn = GameObject.Find("ExperimentStartButton_3D") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
        startBtn.name = "ExperimentStartButton_3D";
        startBtn.transform.SetParent(expObjects.transform);
        startBtn.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
        var startComp = startBtn.GetComponent<ExperimentStartButton>() ?? startBtn.AddComponent<ExperimentStartButton>();
        startComp.controller = controller;
        startComp.modeSwitcher = modeSwitcher;

        // ---- UI ----
        GameObject ui = EnsureOrFindUI(root);
        GameObject canvasPC = EnsureCanvas(ui, "Canvas_PC");
        GameObject canvasVR = EnsureCanvas(ui, "Canvas_VR");
        GameObject labelGO = canvasVR.transform.Find("ModeLabel")?.gameObject ?? CreateText("ModeLabel", canvasVR.transform, "\ud83c\udfae VR\u30e2\u30fc\u30c9");
        var label = labelGO.GetComponent<TextMeshProUGUI>();
        modeSwitcher.pcUIRoot = canvasPC;
        modeSwitcher.vrUIRoot = canvasVR;
        modeSwitcher.modeLabel = label;
        modeSwitcher.experimentButton = startBtn;

        GameObject switchGO = GameObject.Find("ModeSwitchButton_3D") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
        switchGO.name = "ModeSwitchButton_3D";
        switchGO.transform.SetParent(ui.transform);
        var switchComp = switchGO.GetComponent<ModeSwitchButton>() ?? switchGO.AddComponent<ModeSwitchButton>();
        switchComp.modeSwitcher = modeSwitcher;

        // ---- Visuals ----
        GameObject visuals = EnsureChild(root, "Visuals");
        EnsureChild(visuals, "ToolObjects");
        EnsureChild(visuals, "ElementObjects");
        EnsureChild(visuals, "ConditionObjects");

        Debug.Log("\u2705 Hierarchy\u3068\u53c2\u7167\u8a2d\u5b9a\u3092\u691c\u8a3c\u30fb\u4fee\u5fa9\u3057\u307e\u3057\u305f\uff01");
        Selection.activeGameObject = root;
    }

    private static GameObject EnsureChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) return child.gameObject;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    private static T EnsureComponent<T>(GameObject parent, string name) where T : Component
    {
        GameObject go = parent.transform.Find(name)?.gameObject;
        if (go == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent.transform);
        }
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    private static GameObject EnsureCanvas(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        GameObject canvasGO = new GameObject(name);
        canvasGO.transform.SetParent(parent.transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    private static GameObject CreateText(string name, Transform parent, string content)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = 24;
        text.color = Color.white;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
        return go;
    }

    private static GameObject EnsureOrFindUI(GameObject root)
    {
        GameObject ui = root.transform.Find("UI")?.gameObject;
        if (ui != null) return ui;
        ui = GameObject.Find("UI");
        if (ui != null) return ui;
        ui = new GameObject("UI");
        ui.transform.SetParent(root.transform);
        return ui;
    }
}