#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CHEMLABHierarchyBuilder
{
    [MenuItem("CHEMLAB VR/Hierarchyを自動構築する")]
    public static void BuildHierarchy()
    {
        // ルートカテゴリ
        GameObject managers = CreateGroup("Managers");
        GameObject buttons = CreateGroup("Buttons");
        GameObject spawners = CreateGroup("Spawners");
        GameObject monitor = CreateGroup("VR_Monitoring");
        GameObject ui = CreateGroup("UI");
        GameObject data = CreateGroup("Data");
        GameObject experimentTable = CreateGroup("ExperimentTable");

        // 各要素
        var holder = CreateWithComponent("SelectedObjectHolder", typeof(SelectedObjectHolder), data.transform);
        var modeSwitcher = CreateWithComponent("ModeSwitcher", typeof(UdonSharp.UdonSharpBehaviour), managers.transform);
        var controller = CreateWithComponent("ExperimentController", typeof(ExperimentController), managers.transform).GetComponent<ExperimentController>();
        var sender = CreateWithComponent("AIRequestSender", typeof(AIRequestSender), managers.transform).GetComponent<AIRequestSender>();

        var monitorObj = CreateWithComponent("VRExperimentMonitor", typeof(VRExperimentMonitor), monitor.transform).GetComponent<VRExperimentMonitor>();
        var displayManager = CreateWithComponent("CategoryDisplayManager", typeof(CategoryDisplayManager), managers.transform);

        var spawnerBtn = CreateWithComponent("ObjectSpawnerButton", typeof(ObjectSpawnerButton), buttons.transform).GetComponent<ObjectSpawnerButton>();
        var switchBtn = CreateWithComponent("CategorySwitchButton", typeof(CategorySwitchButton), buttons.transform).GetComponent<CategorySwitchButton>();
        var startBtn = CreateWithComponent("ExperimentStartButton", typeof(ExperimentStartButton), buttons.transform).GetComponent<ExperimentStartButton>();

        CreateWithComponent("CompoundSpawner", typeof(UdonSharp.UdonSharpBehaviour), spawners.transform);
        var tableTrigger = CreateWithComponent("ExperimentTableTrigger", typeof(ExperimentTableTrigger), experimentTable.transform).GetComponent<ExperimentTableTrigger>();

        var statusTextObj = CreateText("StatusText", ui.transform);
        var statusText = statusTextObj.GetComponent<Text>();

        // フィールド接続
        if (controller != null)
        {
            controller.holder = holder.GetComponent<SelectedObjectHolder>();
            controller.requestSender = sender;
        }

        if (sender != null)
        {
            sender.monitor = monitorObj;
            sender.statusText = statusText;
        }

        if (spawnerBtn != null)
        {
            spawnerBtn.holder = holder.GetComponent<SelectedObjectHolder>();
            spawnerBtn.modeSwitcher = modeSwitcher.GetComponent<UdonSharp.UdonSharpBehaviour>() as ModeSwitcher;
        }

        if (switchBtn != null)
        {
            switchBtn.spawner = spawnerBtn;
        }

        if (startBtn != null)
        {
            startBtn.controller = controller;
            startBtn.modeSwitcher = modeSwitcher.GetComponent<UdonSharp.UdonSharpBehaviour>() as ModeSwitcher;
            startBtn.experimentTableRoot = experimentTable.transform;
        }

        if (tableTrigger != null)
        {
            tableTrigger.holder = holder.GetComponent<SelectedObjectHolder>();
            tableTrigger.tableRoot = experimentTable.transform;
        }

        Debug.Log("✅ CHEMLAB Hierarchy 構築＋参照接続完了");
    }

    private static GameObject CreateGroup(string name)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, "Create Group " + name);
        return group;
    }

    private static GameObject CreateWithComponent(string name, System.Type componentType, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent(componentType);
        go.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static GameObject CreateText(string name, Transform parent)
    {
        GameObject text = new GameObject(name);
        text.transform.SetParent(parent);
        var uiText = text.AddComponent<Text>();
        uiText.text = name;
        uiText.fontSize = 20;
        Undo.RegisterCreatedObjectUndo(text, "Create Text " + name);
        return text;
    }
}
#endif