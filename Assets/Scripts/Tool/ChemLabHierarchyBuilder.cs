using UnityEditor;
using UnityEngine;

public class ChemLabHierarchyBuilder
{
    [MenuItem("CHEMLAB VR/Build Basic Hierarchy")]
    public static void BuildHierarchy()
    {
        GameObject root = new GameObject("CHEMLAB_VR");

        // GameSystems
        GameObject gameSystems = CreateChild(root, "GameSystems");

        var modeSwitcher = CreateAndAddComponent<ModeSwitcher>(gameSystems, "ModeSwitcher");
        var holder = CreateAndAddComponent<SelectedObjectHolder>(gameSystems, "SelectedObjectHolder");
        var controller = CreateAndAddComponent<ExperimentController>(gameSystems, "ExperimentController");
        var sender = CreateAndAddComponent<AIRequestSender>(gameSystems, "AIRequestSender");
        var handler = CreateAndAddComponent<AIReactionHandler>(gameSystems, "AIReactionHandler");
        var receiver = CreateAndAddComponent<ResultReceiver>(gameSystems, "ResultReceiver");
        var history = CreateAndAddComponent<ExperimentHistory>(gameSystems, "ExperimentHistory");

        // 接続
        controller.requestSender = sender;
        controller.holder = holder;

        sender.handler = handler;

        receiver.history = history;
        receiver.holder = holder;

        handler.spawner = null; // あとで接続
        handler.resultText = null;
        handler.audioSource = null;
        handler.successClip = null;

        // HistorySystem
        GameObject historySystem = CreateChild(root, "HistorySystem");
        var viewer = CreateAndAddComponent<HistoryViewer>(historySystem, "HistoryViewer");
        viewer.history = history;

        // ExperimentObjects
        GameObject expObjects = CreateChild(root, "ExperimentObjects");
        var spawner = CreateAndAddComponent<CompoundSpawner>(expObjects, "CompoundSpawner");
        var executor = CreateAndAddComponent<ExperimentExecutor>(expObjects, "ExperimentExecutor");

        handler.spawner = spawner;

        // 🔘 3D Experiment Start Button を設置（例：Cube）
        GameObject startButton3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startButton3D.name = "ExperimentStartButton_3D";
        startButton3D.transform.SetParent(expObjects.transform);
        startButton3D.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
        var startButton = startButton3D.AddComponent<ExperimentStartButton>();
        startButton.controller = controller;
        startButton.modeSwitcher = modeSwitcher;

        Debug.Log("📁 'StreamingAssets/ExperimentResult.json' は Assets フォルダ内に手動で設置してください。");

        Selection.activeGameObject = root;
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    private static T CreateAndAddComponent<T>(GameObject parent, string name) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go.AddComponent<T>();
    }
}
