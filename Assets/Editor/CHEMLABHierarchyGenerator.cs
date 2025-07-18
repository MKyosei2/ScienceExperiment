#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class CHEMLABHierarchyBuilder
{
    [MenuItem("CHEMLAB VR/Hierarchyを自動構築・接続する")]
    public static void BuildHierarchy()
    {
        GameObject managers = CreateGroup("Managers");
        GameObject buttons = CreateGroup("SelectionButtons");
        GameObject spawners = CreateGroup("Zones");
        GameObject monitor = CreateGroup("VR_Monitoring");
        GameObject ui = CreateGroup("UI");
        GameObject data = CreateGroup("Data");
        GameObject experimentTable = CreateGroup("ExperimentTable");

        var holder = CreateWithComponent("SelectedObjectHolder", typeof(SelectedObjectHolder), data.transform).GetComponent<SelectedObjectHolder>();
        var modeSwitcher = CreateWithComponent("ModeSwitcher", typeof(ModeSwitcher), managers.transform).GetComponent<ModeSwitcher>();
        var controller = CreateWithComponent("ExperimentController", typeof(ExperimentController), managers.transform).GetComponent<ExperimentController>();
        var aiSender = CreateWithComponent("AIRequestSender", typeof(AIRequestSender), managers.transform).GetComponent<AIRequestSender>();
        var vrMonitor = CreateWithComponent("VRExperimentMonitor", typeof(VRExperimentMonitor), monitor.transform).GetComponent<VRExperimentMonitor>();
        var history = CreateWithComponent("ExperimentHistory", typeof(ExperimentHistory), data.transform).GetComponent<ExperimentHistory>();
        var resultReceiver = CreateWithComponent("ResultReceiver", typeof(ResultReceiver), managers.transform).GetComponent<ResultReceiver>();
        var aiHandler = CreateWithComponent("AIReactionHandler", typeof(AIReactionHandler), managers.transform).GetComponent<AIReactionHandler>();

        var modeLabelTMP = CreateTextTMP("ModeLabel", ui.transform).GetComponent<TextMeshProUGUI>();
        var statusTextTMP = CreateTextTMP("StatusText", ui.transform).GetComponent<TextMeshProUGUI>();

        var elementBtn = CreateButtonWithCube("ElementSelectButton", typeof(ZoneSelectionButton), buttons.transform).GetComponent<ZoneSelectionButton>();
        var toolBtn = CreateButtonWithCube("ToolSelectButton", typeof(ZoneSelectionButton), buttons.transform).GetComponent<ZoneSelectionButton>();
        var conditionBtn = CreateButtonWithCube("ConditionSelectButton", typeof(ZoneSelectionButton), buttons.transform).GetComponent<ZoneSelectionButton>();

        var startBtn = CreateButtonWithCube("ExperimentStartButton", typeof(ExperimentStartButton), buttons.transform).GetComponent<ExperimentStartButton>();
        var modeBtn = CreateButtonWithCube("ModeSwitchButton", typeof(ModeSwitchButton), buttons.transform).GetComponent<ModeSwitchButton>();

        var elementZone = CreateSelectionZone("ElementZone", spawners.transform).GetComponent<SelectionZone>();
        var toolZone = CreateSelectionZone("ToolZone", spawners.transform).GetComponent<SelectionZone>();
        var conditionZone = CreateSelectionZone("ConditionZone", spawners.transform).GetComponent<SelectionZone>();

        CreateExperimentZone("ElementExperimentZone", spawners.transform);
        CreateExperimentZone("ToolExperimentZone", spawners.transform);
        CreateExperimentZone("ConditionExperimentZone", spawners.transform);

        CreateWithComponent("ExperimentTableTrigger", typeof(ExperimentTableTrigger), experimentTable.transform);

        toolBtn.transform.localPosition = Vector3.zero;
        elementBtn.transform.localPosition = new Vector3(-1.5f, 0, 0);
        conditionBtn.transform.localPosition = new Vector3(1.5f, 0, 0);

        if (modeSwitcher)
        {
            modeSwitcher.modeLabel = modeLabelTMP;
            modeSwitcher.experimentButton = startBtn.gameObject;
            modeSwitcher.pcUIRoot = buttons;
            modeSwitcher.vrUIRoot = spawners;
            EditorUtility.SetDirty(modeSwitcher);
        }
        if (controller)
        {
            controller.holder = holder;
            controller.requestSender = aiSender;
            EditorUtility.SetDirty(controller);
        }
        if (startBtn)
        {
            startBtn.controller = controller;
            startBtn.modeSwitcher = modeSwitcher;
            startBtn.experimentTableRoot = experimentTable.transform;
            EditorUtility.SetDirty(startBtn);
        }
        if (modeBtn)
        {
            modeBtn.modeSwitcher = modeSwitcher;
            EditorUtility.SetDirty(modeBtn);
        }
        if (resultReceiver)
        {
            resultReceiver.holder = holder;
            resultReceiver.history = history;
            EditorUtility.SetDirty(resultReceiver);
        }
        if (aiSender)
        {
            aiSender.monitor = vrMonitor;
            aiSender.statusText = statusTextTMP;
            EditorUtility.SetDirty(aiSender);
        }
        if (elementBtn)
        {
            elementBtn.holder = holder;
            elementBtn.selectionZone = elementZone;
            EditorUtility.SetDirty(elementBtn);
        }
        if (toolBtn)
        {
            toolBtn.holder = holder;
            toolBtn.selectionZone = toolZone;
            EditorUtility.SetDirty(toolBtn);
        }
        if (conditionBtn)
        {
            conditionBtn.holder = holder;
            conditionBtn.selectionZone = conditionZone;
            EditorUtility.SetDirty(conditionBtn);
        }

        // 🧪 自動プレハブ読み込み＆展開
        string prefabRoot = "Assets/Prefab/RoomAsset";
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabRoot });
        GameObject autoParent = CreateGroup("AutoSpawnedObjects");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = prefab.name;
                instance.transform.SetParent(autoParent.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab " + prefab.name);
            }
        }

        Debug.Log("✅ CHEMLAB VR: Hierarchyの構築と参照接続が完了しました。");
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

    private static GameObject CreateButtonWithCube(string name, System.Type componentType, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.AddComponent(componentType);
        Undo.RegisterCreatedObjectUndo(go, "Create ButtonCube " + name);
        return go;
    }

    private static GameObject CreateTextTMP(string name, Transform parent)
    {
        GameObject text = new GameObject(name);
        text.transform.SetParent(parent);
        text.AddComponent<CanvasRenderer>();
        text.AddComponent<RectTransform>();
        var tmp = text.AddComponent<TextMeshProUGUI>();
        tmp.text = name;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        Undo.RegisterCreatedObjectUndo(text, "Create TMP Text " + name);
        return text;
    }

    private static GameObject CreateSelectionZone(string name, Transform parent)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.AddComponent<SelectionZone>();
        var col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        var rb = zone.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        Undo.RegisterCreatedObjectUndo(zone, "Create Zone " + name);
        return zone;
    }

    private static GameObject CreateExperimentZone(string name, Transform parent)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.tag = "ExperimentZone";
        var col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        var rb = zone.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        Undo.RegisterCreatedObjectUndo(zone, "Create ExperimentZone " + name);
        return zone;
    }
}
#endif
