#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CHEMLABHierarchyBuilder
{
    [MenuItem("CHEMLAB VR/Hierarchyを自動構築する")]
    public static void BuildHierarchy()
    {
        // ルートグループ
        GameObject managers = CreateGroup("Managers");
        GameObject buttons = CreateGroup("SelectionButtons");
        GameObject spawners = CreateGroup("Zones");
        GameObject monitor = CreateGroup("VR_Monitoring");
        GameObject ui = CreateGroup("UI");
        GameObject data = CreateGroup("Data");
        GameObject experimentTable = CreateGroup("ExperimentTable");

        // 各オブジェクト生成
        CreateWithComponent("SelectedObjectHolder", typeof(SelectedObjectHolder), data.transform);
        CreateWithComponent("ModeSwitcher", typeof(ModeSwitcher), managers.transform);
        CreateWithComponent("ExperimentController", typeof(ExperimentController), managers.transform);
        CreateWithComponent("AIRequestSender", typeof(AIRequestSender), managers.transform);
        CreateWithComponent("VRExperimentMonitor", typeof(VRExperimentMonitor), monitor.transform);

        GameObject elementBtn = CreateWithComponent("ElementSelectButton", typeof(ZoneSelectionButton), buttons.transform);
        GameObject toolBtn = CreateWithComponent("ToolSelectButton", typeof(ZoneSelectionButton), buttons.transform);
        GameObject conditionBtn = CreateWithComponent("ConditionSelectButton", typeof(ZoneSelectionButton), buttons.transform);

        CreateWithComponent("ExperimentStartButton", typeof(ExperimentStartButton), buttons.transform);
        CreateWithComponent("ModeSwitchButton", typeof(ModeSwitchButton), buttons.transform);

        CreateSelectionZone("ElementZone", spawners.transform);
        CreateSelectionZone("ToolZone", spawners.transform);
        CreateSelectionZone("ConditionZone", spawners.transform);

        CreateWithComponent("ResultReceiver", typeof(ResultReceiver), managers.transform);
        CreateWithComponent("ExperimentHistory", typeof(ExperimentHistory), data.transform);
        CreateWithComponent("AIReactionHandler", typeof(AIReactionHandler), managers.transform);

        CreateWithComponent("ExperimentTableTrigger", typeof(ExperimentTableTrigger), experimentTable.transform);

        CreateText("StatusText", ui.transform);
        CreateText("ModeLabel", ui.transform);

        // ボタン配置調整（Tool中心）
        if (toolBtn) toolBtn.transform.localPosition = Vector3.zero;
        if (elementBtn) elementBtn.transform.localPosition = new Vector3(-1.5f, 0, 0);
        if (conditionBtn) conditionBtn.transform.localPosition = new Vector3(1.5f, 0, 0);

        Debug.Log("✅ CHEMLAB VR: Hierarchy構築が完了しました。次に 'コンポーネントのフィールドを接続' を実行してください。");
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

    private static void CreateText(string name, Transform parent)
    {
        GameObject text = new GameObject(name);
        text.transform.SetParent(parent);
        text.AddComponent<UnityEngine.UI.Text>();
        Undo.RegisterCreatedObjectUndo(text, "Create Text " + name);
    }

    private static void CreateSelectionZone(string name, Transform parent)
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
    }
}
#endif
