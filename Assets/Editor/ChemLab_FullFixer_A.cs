using UnityEditor;
using UnityEngine;

// ====================================================================
//  ChemLab_FullFixer_A  — FULL WORLD AUTO-REBUILD (A方式)
//  完全に 0 からワールド構成を再構築するための一度きりのツール
// ====================================================================

public class ChemLab_FullFixer_A : EditorWindow
{
    private static GameObject rootSystems;
    private static GameObject rootUI;
    private static GameObject rootWorld;

    [MenuItem("ChemLab/FULL AUTO REBUILD (A方式)")]
    public static void Open()
    {
        GetWindow<ChemLab_FullFixer_A>("ChemLab FULL Fixer (A)");
    }

    private void OnGUI()
    {
        GUILayout.Label("ChemLab FULL Auto-Rebuild", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("🔥 実行する（全部削除 → 0 から再構築）", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog(
                "警告",
                "シーン内の ChemLab 関連オブジェクトをすべて削除し、\n完全に 0 から再構築します。\nよろしいですか？",
                "はい",
                "キャンセル"))
            {
                RunFullRebuild();
            }
        }
    }

    // ====================================================================
    // MAIN ENTRY
    // ====================================================================
    private static void RunFullRebuild()
    {
        Debug.Log("▶ ChemLab Full Fixer (A) — Start");

        DeleteAllChemLabObjects();
        CreateRootFolders();
        CreateCoreSystems();
        CreateSpawner();
        CreateUI();

        Debug.Log("✔ ChemLab Full Fixer 完了：完全再構築されました！");
    }

    // ====================================================================
    // 1. DELETE OLD OBJECTS
    // ====================================================================
    private static void DeleteAllChemLabObjects()
    {
        string[] targets =
        {
            "Systems", "UI", "World",
            "ChemElementSpawner", "ChemElementDatabase",
            "ReactionPredictor", "ChemReactionAnimator"
        };

        foreach (var n in targets)
        {
            var obj = GameObject.Find(n);
            if (obj != null) Object.DestroyImmediate(obj);
        }

        Debug.Log("✔ 既存 ChemLab オブジェクトを削除");
    }

    // ====================================================================
    // 2. ROOT FOLDERS
    // ====================================================================
    private static void CreateRootFolders()
    {
        rootSystems = new GameObject("Systems");
        rootUI = new GameObject("UI");
        rootWorld = new GameObject("World");

        Debug.Log("✔ Root folders 作成");
    }

    // ====================================================================
    // 3. CORE SYSTEMS
    // ====================================================================
    private static void CreateCoreSystems()
    {
        CreateSystemObject("ChemElementDatabase");
        CreateSystemObject("ReactionPredictor");
        CreateSystemObject("ChemReactionAnimator");
        CreateSystemObject("ChemEnvironmentManager");

        Debug.Log("✔ Core Systems 作成");
    }

    private static GameObject CreateSystemObject(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(rootSystems.transform);
        return go;
    }

    // ====================================================================
    // 4. SPAWNER
    // ====================================================================
    private static void CreateSpawner()
    {
        var spawnerRoot = new GameObject("Spawner");
        spawnerRoot.transform.SetParent(rootSystems.transform);

        var spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.SetParent(spawnerRoot.transform);

        var spawner = new GameObject("ChemElementSpawner");
        spawner.transform.SetParent(spawnerRoot.transform);

        spawner.AddComponent<ChemElementSpawner>();

        Debug.Log("✔ Spawner 作成");
    }

    // ====================================================================
    // 5. UI (Selectors + ConditionUI)
    // ====================================================================
    private static readonly string[] ELEMENT_SYMBOLS =
    {
        "H","He","Li","Be","B","C","N","O","F","Ne",
        "Na","Mg","Al","Si","P","S","Cl","Ar","K","Ca",
        "Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
        "Ga","Ge","As","Se","Br","Kr","Rb","Sr","Y","Zr",
        "Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd","In","Sn",
        "Sb","Te","I","Xe","Cs","Ba","La","Ce","Pr","Nd",
        "Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er","Tm","Yb",
        "Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg",
        "Tl","Pb","Bi","Po","At","Rn","Fr","Ra","Ac","Th",
        "Pa","U","Np","Pu","Am","Cm","Bk","Cf","Es","Fm",
        "Md","No","Lr","Rf","Db","Sg","Bh","Hs","Mt","Ds",
        "Rg","Cn","Nh","Fl","Mc","Lv","Ts","Og"
    };

    private static readonly string[] TOOLS =
    {
        "BEAKER","CLaisen_FLASK","CONICAL_FLASK","FLORENCE_FLASK",
        "GASBURNER","RETORT_FLASK","ROUND_BOTTOM_FLASK","VOLUMETRIC_FLASK"
    };

    private static void CreateUI()
    {
        var selectors = new GameObject("Selectors");
        selectors.transform.SetParent(rootUI.transform);

        var elementRoot = new GameObject("Element");
        elementRoot.transform.SetParent(selectors.transform);

        var toolRoot = new GameObject("Tool");
        toolRoot.transform.SetParent(selectors.transform);

        var conditionRoot = new GameObject("Condition");
        conditionRoot.transform.SetParent(rootUI.transform);

        CreateElementButtons(elementRoot);
        CreateToolButtons(toolRoot);
        CreateConditionUI(conditionRoot);

        Debug.Log("✔ UI 再構築");
    }

    // ====================================================================
    // 5-1 ELEMENT BUTTONS
    // ====================================================================
    private static void CreateElementButtons(GameObject parent)
    {
        var spawner = GameObject.Find("ChemElementSpawner").GetComponent<ChemElementSpawner>();

        float spacing = 0.35f;
        int colCount = 12;
        int i = 0;

        foreach (string symbol in ELEMENT_SYMBOLS)
        {
            var btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = symbol;
            btn.transform.SetParent(parent.transform);

            int row = i / colCount;
            int col = i % colCount;

            btn.transform.localPosition = new Vector3(col * spacing, 0, -row * spacing);
            btn.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);

            var txt = Create3DText(btn.transform, symbol);
            txt.transform.localPosition = new Vector3(0, 0.25f, 0);

            var b = btn.AddComponent<SpawnSelectorButton>();
            b.spawner = spawner;
            b.type = "Element";
            b.targetName = symbol;

            i++;
        }

        parent.transform.localPosition = new Vector3(-2.2f, 1.2f, 3f);
        Debug.Log("✔ 118 Element Buttons 作成");
    }

    // ====================================================================
    // 5-2 TOOL BUTTONS
    // ====================================================================
    private static void CreateToolButtons(GameObject parent)
    {
        var spawner = GameObject.Find("ChemElementSpawner").GetComponent<ChemElementSpawner>();

        float spacing = 0.5f;

        for (int i = 0; i < TOOLS.Length; i++)
        {
            var btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn.name = TOOLS[i];
            btn.transform.SetParent(parent.transform);

            btn.transform.localPosition = new Vector3(i * spacing, 0, 0);
            btn.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);

            var txt = Create3DText(btn.transform, TOOLS[i]);
            txt.transform.localPosition = new Vector3(0, 0.25f, 0);

            var b = btn.AddComponent<SpawnSelectorButton>();
            b.spawner = spawner;
            b.type = "Equipment";
            b.targetName = TOOLS[i];
        }

        parent.transform.localPosition = new Vector3(-2.2f, 0.5f, 3f);
        Debug.Log("✔ Tool Buttons 作成");
    }

    // ====================================================================
    // 5-3 CONDITION UI
    // ====================================================================
    private static void CreateConditionUI(GameObject root)
    {
        CreateConditionSet(root, "temperature", 0.0f);
        CreateConditionSet(root, "humidity", -0.5f);
        CreateConditionSet(root, "pressure", -1.0f);

        root.transform.localPosition = new Vector3(2.0f, 1.2f, 3f);
        Debug.Log("✔ Condition UI 作成");
    }

    private static void CreateConditionSet(GameObject parent, string name, float yOffset)
    {
        var set = new GameObject(name);
        set.transform.SetParent(parent.transform);
        set.transform.localPosition = new Vector3(0, yOffset, 0);

        var label = Create3DText(set.transform, name);
        label.transform.localPosition = new Vector3(0, 0.25f, 0);

        CreatePlusMinus(set.transform, name, "+", 0.25f);
        CreatePlusMinus(set.transform, name, "-", -0.25f);

        var value = Create3DText(set.transform, "0");
        value.name = name + "_value";
        value.transform.localPosition = Vector3.zero;
    }

    private static void CreatePlusMinus(Transform parent, string name, string op, float x)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = $"{name}_{op}";
        b.transform.SetParent(parent);
        b.transform.localPosition = new Vector3(x, 0, 0);
        b.transform.localScale = Vector3.one * 0.15f;

        var t = Create3DText(b.transform, op);
        t.transform.localPosition = new Vector3(0, 0.2f, 0);
    }

    // ====================================================================
    // UTIL: 3D TEXT
    // ====================================================================
    private static GameObject Create3DText(Transform parent, string text)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 70;
        tm.characterSize = 0.05f;
        tm.color = Color.white;
        tm.anchor = TextAnchor.MiddleCenter;

        return go;
    }
}

