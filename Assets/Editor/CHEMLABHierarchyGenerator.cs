using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CHEMLABHierarchyGenerator
{
    [MenuItem("CHEMLAB VR/初期Hierarchyを生成（親なしコンポーネント付き）")]
    public static void CreateCHEMLABHierarchy()
    {
        // ─── WorldCanvas ───
        GameObject canvas = new GameObject("WorldCanvas", typeof(Canvas));
        Canvas canvasComp = canvas.GetComponent<Canvas>();
        canvasComp.renderMode = RenderMode.WorldSpace;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        GameObject modeLabel = new GameObject("ModeLabel", typeof(TextMeshProUGUI));
        modeLabel.transform.SetParent(canvas.transform);
        var modeLabelText = modeLabel.GetComponent<TextMeshProUGUI>();
        modeLabelText.text = "🎮 VRモード";
        modeLabelText.fontSize = 24;

        // ─── Managers ───
        GameObject managers = new GameObject("Managers");

        CreateWithComponent<ModeSwitcher>("ModeSwitcher", managers.transform);
        CreateWithComponent<ExperimentController>("ExperimentController", managers.transform);
        CreateWithComponent<AIRequestSender>("AIRequestSender", managers.transform);
        CreateWithComponent<AIReactionHandler>("AIReactionHandler", managers.transform);
        CreateWithComponent<ResultReceiver>("ResultReceiver", managers.transform);
        CreateWithComponent<ExperimentHistory>("ExperimentHistory", managers.transform);

        // ─── Zones ───
        GameObject zones = new GameObject("Zones");

        CreateZone("ElementZone", zones.transform, "Element");
        CreateZone("ToolZone", zones.transform, "Tool");
        CreateZone("ConditionZone", zones.transform, "Condition");

        // ─── SelectionButtons（3D） ───
        GameObject selBtns = new GameObject("SelectionButtons");

        CreateButtonWithComponent<ZoneSelectionButton>("ElementSelectButton", selBtns.transform);
        CreateButtonWithComponent<ZoneSelectionButton>("ToolSelectButton", selBtns.transform);
        CreateButtonWithComponent<ZoneSelectionButton>("ConditionSelectButton", selBtns.transform);

        // ─── ActionButtons（3D） ───
        GameObject actBtns = new GameObject("ActionButtons");

        CreateButtonWithComponent<ExperimentStartButton>("ExperimentStartButton", actBtns.transform);
        CreateButtonWithComponent<ModeSwitchButton>("ModeSwitchButton", actBtns.transform);

        // ─── Spawners ───
        GameObject spawners = new GameObject("Spawners");

        CreateWithComponent<CompoundSpawner>("CompoundSpawner", spawners.transform);
        CreateWithComponent<ExperimentExecutor>("ExperimentExecutor", spawners.transform);

        // ─── Data ───
        GameObject data = new GameObject("Data");

        CreateWithComponent<SelectedObjectHolder>("SelectedObjectHolder", data.transform);
    }

    private static void CreateZone(string name, Transform parent, string zoneType)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        var collider = zone.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        zone.AddComponent<SelectionZone>();

        GameObject spawner = new GameObject("ZoneSelectionSpawner");
        spawner.transform.SetParent(zone.transform);
        var zs = spawner.AddComponent<ZoneSelectionSpawner>();
        zs.zoneType = zoneType;
    }

    private static void CreateButtonWithComponent<T>(string name, Transform parent) where T : Component
    {
        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = name;
        button.transform.SetParent(parent);
        Object.DestroyImmediate(button.GetComponent<Collider>()); // デフォルト削除
        button.AddComponent<BoxCollider>().isTrigger = true;
        button.AddComponent<T>();
        var rb = button.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private static void CreateWithComponent<T>(string name, Transform parent) where T : Component
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.AddComponent<T>();
    }
}
