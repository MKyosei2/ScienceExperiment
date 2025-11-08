using UnityEngine;
using UnityEditor;

/// <summary>
/// VRC 大化学実験用 – 一度きりで全部配線する Inspector 自動設定ツール
/// 既にシーンに存在するコンポーネントだけを対象に、参照フィールドを一括設定します。
/// 新しいコンポーネントは一切追加しません。
/// </summary>
public class VRChemLabAutoConfigurator : EditorWindow
{
    // 中核コンポーネント（自動検出＋必要なら手動指定）
    private ChemElementSpawner spawner;
    private ChemEnvironmentManager envManager;
    private EnvUISyncBridge uiSync;
    private JsonReactionPlayer jsonPlayer;

    // Hierarchyのルート（自動検出＋必要なら手動指定）
    private Transform systemsRoot;
    private Transform spawnerRoot;
    private Transform elementButtonsRoot;
    private Transform toolButtonsRoot;
    private Transform conditionButtonsRoot;

    [MenuItem("VRC ChemLab/Auto Setup/Run Full Auto Configurator")]
    public static void Open()
    {
        GetWindow<VRChemLabAutoConfigurator>("VRChemLab Auto Configurator");
    }

    private void OnEnable()
    {
        // 可能な範囲で自動推測しておく（足りないところはユーザーがドラッグ）
        if (spawner == null) spawner = FindObjectOfType<ChemElementSpawner>(true);
        if (envManager == null) envManager = FindObjectOfType<ChemEnvironmentManager>(true);
        if (uiSync == null) uiSync = FindObjectOfType<EnvUISyncBridge>(true);
        if (jsonPlayer == null) jsonPlayer = FindObjectOfType<JsonReactionPlayer>(true);

        if (systemsRoot == null) systemsRoot = FindRootByNameContains("Systems");
        if (spawnerRoot == null) spawnerRoot = FindRootByNameContains("Spawner");
        if (elementButtonsRoot == null) elementButtonsRoot = FindRootByNameContains("Element");
        if (toolButtonsRoot == null) toolButtonsRoot = FindRootByNameContains("Tool");
        if (conditionButtonsRoot == null) conditionButtonsRoot = FindRootByNameContains("Condition");
    }

    private void OnGUI()
    {
        GUILayout.Label("VRC 大化学実験 – 一度きりの全自動 Inspector 設定ツール", EditorStyles.boldLabel);
        GUILayout.Space(6);

        EditorGUILayout.LabelField("▼ 中核コンポーネント（足りない場合はドラッグで指定）", EditorStyles.boldLabel);
        spawner = (ChemElementSpawner)EditorGUILayout.ObjectField("ChemElementSpawner", spawner, typeof(ChemElementSpawner), true);
        envManager = (ChemEnvironmentManager)EditorGUILayout.ObjectField("ChemEnvironmentManager", envManager, typeof(ChemEnvironmentManager), true);
        uiSync = (EnvUISyncBridge)EditorGUILayout.ObjectField("EnvUISyncBridge", uiSync, typeof(EnvUISyncBridge), true);
        jsonPlayer = (JsonReactionPlayer)EditorGUILayout.ObjectField("JsonReactionPlayer", jsonPlayer, typeof(JsonReactionPlayer), true);

        GUILayout.Space(6);
        EditorGUILayout.LabelField("▼ Hierarchy の親（ドラッグで指定すると精度アップ）", EditorStyles.boldLabel);
        systemsRoot = (Transform)EditorGUILayout.ObjectField("Systems Root", systemsRoot, typeof(Transform), true);
        spawnerRoot = (Transform)EditorGUILayout.ObjectField("Spawner Root", spawnerRoot, typeof(Transform), true);
        elementButtonsRoot = (Transform)EditorGUILayout.ObjectField("Element Buttons Root", elementButtonsRoot, typeof(Transform), true);
        toolButtonsRoot = (Transform)EditorGUILayout.ObjectField("Tool Buttons Root", toolButtonsRoot, typeof(Transform), true);
        conditionButtonsRoot = (Transform)EditorGUILayout.ObjectField("Condition Buttons Root", conditionButtonsRoot, typeof(Transform), true);

        GUILayout.Space(10);

        if (GUILayout.Button("⚙ 一括自動設定を実行", GUILayout.Height(40)))
        {
            RunAutoConfig();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "・AddComponent は一切行わず、既にアタッチされているコンポーネントだけを対象にします。\n" +
            "・Hierarchy 上で Element / Tool / Condition の親を正しく指定すると、ボタン分類がほぼ完全に合います。\n" +
            "・1回実行すれば、Spawner / 環境 / UI / 各ボタンがすべて配線される想定です。",
            MessageType.Info
        );
    }

    private void RunAutoConfig()
    {
        Debug.Log("=== [VRChemLabAutoConfigurator] 自動設定開始 ===");

        // 代表コンポーネントの自動検出フォールバック
        if (spawner == null) spawner = FindObjectOfType<ChemElementSpawner>(true);
        if (envManager == null) envManager = FindObjectOfType<ChemEnvironmentManager>(true);
        if (uiSync == null) uiSync = FindObjectOfType<EnvUISyncBridge>(true);
        if (jsonPlayer == null) jsonPlayer = FindObjectOfType<JsonReactionPlayer>(true);

        if (spawner == null)
        {
            Debug.LogError("[AutoConfig] ❌ ChemElementSpawner が見つかりません。");
            return;
        }
        if (envManager == null)
        {
            Debug.LogError("[AutoConfig] ❌ ChemEnvironmentManager が見つかりません。");
            return;
        }
        if (uiSync == null)
        {
            Debug.LogError("[AutoConfig] ❌ EnvUISyncBridge が見つかりません。");
            return;
        }

        // -------- 1) Spawner の設定 --------
        SetupSpawner();

        // -------- 2) SpawnSelectorButton（元素/器具）の設定 --------
        SetupSpawnSelectorButtons();

        // -------- 3) Start / Reset ボタン --------
        SetupStartResetButtons();

        // -------- 4) ValueAdjustButton（環境） --------
        SetupValueAdjustButtons();

        // -------- 5) EnvUISyncBridge --------
        SetupEnvUISyncBridge();

        // -------- 6) Orchestrator / VRMonitor / AI --------
        SetupExperimentOrchestrator();
        SetupVRExperimentMonitor();
        SetupAIRequestSender();

        AssetDatabase.SaveAssets();
        Debug.Log("=== [VRChemLabAutoConfigurator] 🎉 全自動設定完了！ ===");
    }

    // ======================================================
    // 1) Spawner
    // ======================================================
    private void SetupSpawner()
    {
        Undo.RecordObject(spawner, "AutoConfig Spawner");

        // spawnParent
        if (spawner.spawnParent == null)
        {
            if (spawnerRoot != null)
            {
                spawner.spawnParent = spawnerRoot;
            }
            else
            {
                // Systems/Spawner 的なものを探す
                var s = FindRootByNameContains("Spawner");
                if (s != null) spawner.spawnParent = s;
                else spawner.spawnParent = spawner.transform;
            }
        }

        // sourceVessel
        if (spawner.sourceVessel == null)
        {
            GameObject candidate = null;

            // SpawnerRoot 配下から “Source / Flask / Vessel / Beaker” を含む名前を探す
            Transform searchRoot = spawnerRoot != null ? spawnerRoot : spawner.spawnParent;
            if (searchRoot != null)
            {
                foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name.ToLower();
                    if (n.Contains("source") || n.Contains("flask") || n.Contains("vessel") || n.Contains("beaker"))
                    {
                        candidate = t.gameObject;
                        break;
                    }
                }
            }

            if (candidate != null)
            {
                spawner.sourceVessel = candidate;
                Debug.Log("[AutoConfig] Spawner.sourceVessel を設定: " + GetFullPath(candidate.transform));
            }
            else
            {
                Debug.LogWarning("[AutoConfig] 共通器具 (sourceVessel) の候補が見つかりませんでした。Spawner の sourceVessel を手動で設定してください。");
            }
        }

        // reactionPlayer
        if (spawner.reactionPlayer == null && jsonPlayer != null)
        {
            spawner.reactionPlayer = jsonPlayer;
            Debug.Log("[AutoConfig] Spawner.reactionPlayer を設定: " + GetFullPath(jsonPlayer.transform));
        }

        EditorUtility.SetDirty(spawner);
        Debug.Log("[AutoConfig] ✅ ChemElementSpawner 設定完了");
    }

    // ======================================================
    // 2) SpawnSelectorButton（元素 / 器具）
    // ======================================================
    private void SetupSpawnSelectorButtons()
    {
        var allButtons = FindObjectsOfType<SpawnSelectorButton>(true);
        int count = 0;

        foreach (var btn in allButtons)
        {
            Undo.RecordObject(btn, "AutoConfig SpawnSelectorButton");

            // Spawner
            btn.spawner = spawner;

            // 親階層でカテゴリ判定（Hierarchyベース）
            string type = btn.type;
            SelectionCategory cat = btn.category;
            string catName = btn.categoryName;

            Transform t = btn.transform;

            if (elementButtonsRoot != null && t.IsChildOf(elementButtonsRoot))
            {
                type = "Element";
                cat = SelectionCategory.Element;
                catName = "Element";
            }
            else if (toolButtonsRoot != null && t.IsChildOf(toolButtonsRoot))
            {
                type = "Equipment";
                cat = SelectionCategory.Tool;
                catName = "Tool";
            }
            else if (conditionButtonsRoot != null && t.IsChildOf(conditionButtonsRoot))
            {
                type = "Condition";
                cat = SelectionCategory.Condition;
                catName = "Condition";
            }
            else
            {
                // 名前から簡易判定（Element / Tool / Condition を含む親を探す）
                Transform p = t.parent;
                while (p != null)
                {
                    string pn = p.name.ToLower();
                    if (pn.Contains("element"))
                    {
                        type = "Element";
                        cat = SelectionCategory.Element;
                        catName = "Element";
                        break;
                    }
                    if (pn.Contains("tool") || pn.Contains("equip"))
                    {
                        type = "Equipment";
                        cat = SelectionCategory.Tool;
                        catName = "Tool";
                        break;
                    }
                    if (pn.Contains("cond") || pn.Contains("env"))
                    {
                        type = "Condition";
                        cat = SelectionCategory.Condition;
                        catName = "Condition";
                        break;
                    }
                    p = p.parent;
                }
            }

            btn.type = type;
            btn.category = cat;
            btn.categoryName = catName;

            if (string.IsNullOrEmpty(btn.targetName))
                btn.targetName = btn.gameObject.name;

            EditorUtility.SetDirty(btn);
            count++;
        }

        Debug.Log($"[AutoConfig] ✅ SpawnSelectorButton 設定完了: {count} 件");
    }

    // ======================================================
    // 3) Start / Reset ボタン
    // ======================================================
    private void SetupStartResetButtons()
    {
        var starts = FindObjectsOfType<StartExperimentButton>(true);
        foreach (var s in starts)
        {
            Undo.RecordObject(s, "AutoConfig StartExperimentButton");
            s.spawner = spawner;
            EditorUtility.SetDirty(s);
            Debug.Log("[AutoConfig] StartExperimentButton.spawner 設定: " + GetFullPath(s.transform));
        }

        var resets = FindObjectsOfType<ResetExperimentButton>(true);
        foreach (var r in resets)
        {
            Undo.RecordObject(r, "AutoConfig ResetExperimentButton");
            r.spawner = spawner;
            r.envManager = envManager;
            r.uiSync = uiSync;
            EditorUtility.SetDirty(r);
            Debug.Log("[AutoConfig] ResetExperimentButton 設定: " + GetFullPath(r.transform));
        }

        Debug.Log("[AutoConfig] ✅ Start / Reset ボタン設定完了");
    }

    // ======================================================
    // 4) ValueAdjustButton
    // ======================================================
    private void SetupValueAdjustButtons()
    {
        var vals = FindObjectsOfType<ValueAdjustButton>(true);
        int count = 0;
        foreach (var v in vals)
        {
            Undo.RecordObject(v, "AutoConfig ValueAdjustButton");
            v.envManager = envManager;
            EditorUtility.SetDirty(v);
            count++;
        }
        Debug.Log($"[AutoConfig] ✅ ValueAdjustButton 設定完了: {count} 件");
    }

    // ======================================================
    // 5) EnvUISyncBridge
    // ======================================================
    private void SetupEnvUISyncBridge()
    {
        Undo.RecordObject(uiSync, "AutoConfig EnvUISyncBridge");

        if (uiSync.manager == null)
            uiSync.manager = envManager;

        EditorUtility.SetDirty(uiSync);
        Debug.Log("[AutoConfig] ✅ EnvUISyncBridge 設定完了");
    }

    // ======================================================
    // 6) ExperimentOrchestrator / VRMonitor / AIRequestSender
    // ======================================================
    private void SetupExperimentOrchestrator()
    {
        var orchs = FindObjectsOfType<ExperimentOrchestrator>(true);
        foreach (var o in orchs)
        {
            Undo.RecordObject(o, "AutoConfig ExperimentOrchestrator");
            o.spawner = spawner;
            o.environmentManager = envManager;
            o.uiSync = uiSync;
            EditorUtility.SetDirty(o);
            Debug.Log("[AutoConfig] ExperimentOrchestrator 設定: " + GetFullPath(o.transform));
        }
    }

    private void SetupVRExperimentMonitor()
    {
        var mons = FindObjectsOfType<VRExperimentMonitor>(true);
        foreach (var m in mons)
        {
            Undo.RecordObject(m, "AutoConfig VRExperimentMonitor");
            m.spawner = spawner;
            EditorUtility.SetDirty(m);
            Debug.Log("[AutoConfig] VRExperimentMonitor 設定: " + GetFullPath(m.transform));
        }
    }

    private void SetupAIRequestSender()
    {
        var ais = FindObjectsOfType<AIRequestSender>(true);
        foreach (var a in ais)
        {
            Undo.RecordObject(a, "AutoConfig AIRequestSender");
            a.spawner = spawner;
            EditorUtility.SetDirty(a);
            Debug.Log("[AutoConfig] AIRequestSender 設定: " + GetFullPath(a.transform));
        }
    }

    // ======================================================
    // Utility
    // ======================================================
    private Transform FindRootByNameContains(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        key = key.ToLower();

        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var go = roots[i];
            if (go.name.ToLower().Contains(key))
                return go.transform;

            // Selectors/Element みたいなネストも念のため見る
            var trs = go.GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < trs.Length; j++)
            {
                if (trs[j].name.ToLower().Contains(key))
                    return trs[j];
            }
        }
        return null;
    }

    private static string GetFullPath(Transform t)
    {
        if (t == null) return "(null)";
        string path = t.name;
        for (Transform p = t.parent; p != null; p = p.parent)
            path = p.name + "/" + path;
        return path;
    }
}
