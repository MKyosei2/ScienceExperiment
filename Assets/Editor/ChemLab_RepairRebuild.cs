using UnityEditor;
using UnityEngine;
using UdonSharp;
using System.Collections.Generic;
using TMPro;

public class ChemLab_RepairRebuild : EditorWindow
{
    // Root Objects
    private static GameObject root;
    private static GameObject worldRoot;
    private static GameObject systemsRoot;
    private static GameObject spawnerRoot;
    private static GameObject uiRoot;

    private static ChemElementSpawner spawner;
    private const string ROOT_NAME = "ChemLabRoot";

    private const string PREFAB_TOOLS_PATH = "Assets/Prefabs/Tool/";

    [MenuItem("Tools/ChemLab / Auto Rebuild")]
    public static void Open()
    {
        GetWindow<ChemLab_RepairRebuild>("ChemLab Auto-Rebuild");
    }

    void OnGUI()
    {
        GUILayout.Label("ChemLab Auto-Rebuild Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("実行：ChemLabを修復＋再構築", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("確認",
                "ChemLab関連のオブジェクトを再構築します。\nWorld（床・壁）は維持されます。\n実行しますか？",
                "はい", "キャンセル"))
            {
                RunFullRepair();
            }
        }
    }

    // ===============================================================
    // Main Entry
    // ===============================================================
    private static void RunFullRepair()
    {
        Debug.Log("=== ChemLab Auto-Rebuild 開始 ===");

        RemoveOld();
        CreateRoots();
        CreateSystems();
        CreateSpawner();
        CreateUI();

        Debug.Log("=== ChemLab Auto-Rebuild 完了 ===");
    }

    // ===============================================================
    // 1. Remove old ChemLab parts, keep World
    // ===============================================================
    private static void RemoveOld()
    {
        // 既存根を削除
        var old = GameObject.Find(ROOT_NAME);
        if (old != null) Object.DestroyImmediate(old);

        // UI/Systems/Spawnerの名残だけ削除
        string[] names = { "UI", "Systems", "Spawner" };
        foreach (string n in names)
        {
            var target = GameObject.Find(n);
            if (target != null && target.transform.parent == null)
                Object.DestroyImmediate(target);
        }
    }

    // ===============================================================
    // 2. Create Root Structure
    // ===============================================================
    private static void CreateRoots()
    {
        root = new GameObject(ROOT_NAME);

        // World (Floor / Walls) を維持して取り込む
        var w = GameObject.Find("World");
        if (w != null)
        {
            worldRoot = w;
            worldRoot.transform.SetParent(root.transform);
        }

        systemsRoot = new GameObject("Systems");
        systemsRoot.transform.SetParent(root.transform);

        spawnerRoot = new GameObject("Spawner");
        spawnerRoot.transform.SetParent(root.transform);

        uiRoot = new GameObject("UI");
        uiRoot.transform.SetParent(root.transform);
    }

    // ===============================================================
    // 3. Systems
    // ===============================================================
    private static void CreateSystems()
    {
        AddSystemObject<ChemElementDatabase>("ChemElementDatabase");
        AddSystemObject<ReactionPredictor>("ReactionPredictor");
        AddSystemObject<ChemEnvironmentManager>("ChemEnvironmentManager");
        AddSystemObject<ChemReactionAnimator>("ChemReactionAnimator");
    }

    private static GameObject AddSystemObject<T>(string name) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(systemsRoot.transform);
        go.AddComponent<T>();
        return go;
    }

    // ===============================================================
    // 4. Spawner
    // ===============================================================
    private static void CreateSpawner()
    {
        var spPoint = new GameObject("SpawnPoint");
        spPoint.transform.SetParent(spawnerRoot.transform);

        var spObj = new GameObject("ChemElementSpawner");
        spObj.transform.SetParent(spawnerRoot.transform);

        spawner = spObj.AddComponent<ChemElementSpawner>();
        spawner.spawnParent = spPoint.transform;

        // link systems
        spawner.db = systemsRoot.transform.Find("ChemElementDatabase").GetComponent<ChemElementDatabase>();
        spawner.predictor = systemsRoot.transform.Find("ReactionPredictor").GetComponent<ReactionPredictor>();
        spawner.animator = systemsRoot.transform.Find("ChemReactionAnimator").GetComponent<ChemReactionAnimator>();

        // Flask Prefab
        var flask = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CONICAL_FLASK.prefab");
        if (flask)
            spawner.sourceVessel = flask;
    }

    // ===============================================================
    // 5. UI
    // ===============================================================
    private static void CreateUI()
    {
        GameObject selectorRoot = new GameObject("Selectors");
        selectorRoot.transform.SetParent(uiRoot.transform);

        // ELEMENT BUTTONS
        var elementRoot = new GameObject("Element");
        elementRoot.transform.SetParent(selectorRoot.transform);
        BuildElementButtons(elementRoot);

        // TOOL BUTTONS
        var toolRoot = new GameObject("Tool");
        toolRoot.transform.SetParent(selectorRoot.transform);
        BuildToolButtons(toolRoot);

        // CONDITION UI
        var condRoot = new GameObject("Condition");
        condRoot.transform.SetParent(selectorRoot.transform);
        BuildConditionUI(condRoot);
    }

    // ===============================================================
    // Element Buttons（118個）
    // ===============================================================
    private static void BuildElementButtons(GameObject parent)
    {
        var db = systemsRoot.transform.Find("ChemElementDatabase").GetComponent<ChemElementDatabase>();
        string[] symbols = db.AllElementSymbols;   // ← 修正済

        int x = 0, y = 0;
        const float sx = 0.25f;
        const float sy = -0.25f;

        foreach (string s in symbols)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent.transform);
            cube.name = s;

            cube.transform.localPosition = new Vector3(x * sx, y * sy, 0);
            cube.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            var btn = cube.AddComponent<SpawnSelectorButton>();
            btn.spawner = spawner;
            btn.type = "Element";
            btn.targetName = s;

            x++;
            if (x >= 18) { x = 0; y++; }
        }
    }

    // ===============================================================
    // Tool Buttons（Prefab → Unpack）
    // ===============================================================
    private static void BuildToolButtons(GameObject parent)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { PREFAB_TOOLS_PATH });

        float x = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            GameObject inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            inst.transform.SetParent(parent.transform);
            inst.transform.localPosition = new Vector3(x * 0.5f, 0, 0);
            inst.transform.localScale = Vector3.one * 0.2f;
            x++;

            // clear broken components except MeshFilter/MeshRenderer/Collider/Transform
            CleanToolObject(inst);

            // button
            var btn = inst.AddComponent<SpawnSelectorButton>();
            btn.spawner = spawner;
            btn.type = "Equipment";
            btn.targetName = prefab.name;
        }
    }

    private static void CleanToolObject(GameObject obj)
    {
        Component[] comps = obj.GetComponentsInChildren<Component>();
        foreach (Component c in comps)
        {
            if (!c) continue;
            var t = c.GetType();
            if (t == typeof(Transform) ||
                t == typeof(MeshRenderer) ||
                t == typeof(MeshFilter) ||
                t == typeof(BoxCollider))
                continue;

            if (t.BaseType != typeof(UdonSharpBehaviour))
            {
                Object.DestroyImmediate(c);
            }
        }
    }

    // ===============================================================
    // Condition UI（温度 / 湿度 / 圧力）
    // ===============================================================
    private static void BuildConditionUI(GameObject root)
    {
        string[] names = { "temperature", "humidity", "pressure" };

        var env = systemsRoot.transform.Find("ChemEnvironmentManager").GetComponent<ChemEnvironmentManager>();

        foreach (string n in names)
        {
            GameObject wrap = new GameObject(n);
            wrap.transform.SetParent(root.transform);

            // Minus Button
            var minus = GameObject.CreatePrimitive(PrimitiveType.Cube);
            minus.transform.SetParent(wrap.transform);
            minus.transform.localPosition = new Vector3(-0.25f, 0, 0);

            var adj0 = minus.AddComponent<ConditionAdjuster>();
            adj0.mode = n;
            adj0.value = -1;
            adj0.env = env;

            // Plus Button
            var plus = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plus.transform.SetParent(wrap.transform);
            plus.transform.localPosition = new Vector3(0.25f, 0, 0);

            var adj1 = plus.AddComponent<ConditionAdjuster>();
            adj1.mode = n;
            adj1.value = +1;
            adj1.env = env;

            // Display
            var disp = new GameObject("Display");
            disp.transform.SetParent(wrap.transform);
            disp.transform.localPosition = Vector3.zero;

            // Text (TMP)
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(disp.transform);

            var tm = textGO.AddComponent<TextMeshPro>();
            tm.fontSize = 4f;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;
            tm.text = "0";

            // Script Link
            var displayScript = disp.AddComponent<ConditionDisplay>();
            displayScript.mode = n;
            displayScript.env = env;
            displayScript.text = tm;     // ← ★ TMP_Text
        }
    }
}
