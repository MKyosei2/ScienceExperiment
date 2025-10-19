#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TMPro;

public class VRChemLabHierarchyAutoSetup : EditorWindow
{
    [MenuItem("VRC Lab/Auto Setup (写真Hierarchyに同期)")]
    public static void Open() => GetWindow<VRChemLabHierarchyAutoSetup>("VRC ChemLab AutoSetup");

    private void OnGUI()
    {
        GUILayout.Label("VRC大化学実験 – Inspector自動設定（写真のHierarchy準拠）", EditorStyles.boldLabel);
        if (GUILayout.Button("実行（1回だけでOK）", GUILayout.Height(32)))
        {
            Apply();
        }
    }

    // ===== 実行本体 =====
    private static void Apply()
    {
        // --- 1) 主要オブジェクトを写真の絶対パスで取得 ---
        var go_ExperimentOrchestrator = FindAt("Systems/GameState/ExperimentOrchestrator");
        var go_ChemEnvManager = FindAt("Systems/ChemEnvironmentManager");
        var go_SpawnerRoot = FindAt("Systems/Spawner");
        var go_ChemElementSpawner = FindAt("Systems/Spawner/ChemElementSpawner");
        var go_SpawnPoint = FindAt("Systems/Spawner/SpawnPoint");
        var go_VRMonitor = FindAt("Systems/ModeSystem/VRExperimentMonitor");
        var go_ModeRouter = FindAt("Systems/ModeSystem/ModeRouter");
        var go_JsonReactionPlayer = FindAt("Systems/ReactionSystem/JsonReactionPlayer");
        var go_VisualExperimentPlayer = FindAt("Systems/ReactionSystem/VisualExperimentPlayer");
        var go_ChemVisualController = FindAt("Systems/ReactionSystem/ChemVisualController");
        var go_AIRequestSender = FindAt("Systems/AISystem/AIRequestSender");
        var go_ExperimentTableZones = FindAt("Systems/Interactables/ExperimentTable/Zones");

        var go_UI_Root = FindAt("UI");
        var go_UI_Selectors_Element = FindAt("UI/Selectors/Element");
        var go_UI_Selectors_Tool = FindAt("UI/Selectors/Tool");
        var go_UI_HUD_OutputText = FindAt("UI/HUD/OutputText");
        var go_UI_Buttons_Start = FindAt("UI/Buttons/StartButton");
        var go_UI_Buttons_Reset = FindAt("UI/Buttons/ResetButton");

        var go_UI_Cond_root = FindAt("UI/Category/Condition");
        var go_UI_temp_field = FindAt("UI/Category/Condition/temperature/temperature");
        var go_UI_pres_field = FindAt("UI/Category/Condition/pressure/pressure");
        var go_UI_humi_field = FindAt("UI/Category/Condition/humidity/humidity");

        // --- 2) 必須チェック ---
        if (!go_ChemElementSpawner || !go_ChemEnvManager || !go_ExperimentOrchestrator || !go_UI_Cond_root)
        {
            Debug.LogError("[AutoSetup] 必須オブジェクトが見つかりません。写真のHierarchy名と一致しているか確認してください。");
            return;
        }

        // --- 3) コンポーネント取得 ---
        var spawner = go_ChemElementSpawner.GetComponent<ChemElementSpawner>();
        var envManager = go_ChemEnvManager.GetComponent<ChemEnvironmentManager>();
        var orchestrator = go_ExperimentOrchestrator.GetComponent<ExperimentOrchestrator>();
        var vrMonitor = go_VRMonitor ? go_VRMonitor.GetComponent<VRExperimentMonitor>() : null;
        var aiSender = go_AIRequestSender ? go_AIRequestSender.GetComponent<AIRequestSender>() : null;
        var jsonPlayer = go_JsonReactionPlayer ? go_JsonReactionPlayer.GetComponent<JsonReactionPlayer>() : null;
        var uiSync = go_UI_Cond_root.GetComponent<EnvUISyncBridge>();
        var startBtnComp = go_UI_Buttons_Start ? go_UI_Buttons_Start.GetComponent<StartExperimentButton>() : null;
        var resetBtnComp = go_UI_Buttons_Reset ? go_UI_Buttons_Reset.GetComponent<ResetExperimentButton>() : null;

        // --- 4) Spawnerの参照を設定 ---
        if (spawner)
        {
            Undo.RecordObject(spawner, "Setup ChemElementSpawner");
            // spawnParent は写真通り Spawner ルートを使う
            spawner.spawnParent = go_SpawnerRoot ? go_SpawnerRoot.transform : spawner.transform;

            // Reaction プレイヤー
            if (jsonPlayer) spawner.reactionPlayer = jsonPlayer;

            // NOTE: conicalFlaskPrefab / beakerPrefab / wireMaterial は各自のアセットを割り当ててください。
            // ここでは自動割り当てを強制しません（名前違い事故回避）。
        }

        // --- 5) EnvUISyncBridge（UI ←→ 環境） ---
        if (uiSync && envManager)
        {
            Undo.RecordObject(uiSync, "Setup EnvUISyncBridge");
            uiSync.manager = envManager;

            if (go_UI_temp_field) uiSync.tempField = go_UI_temp_field.GetComponent<TMP_InputField>();
            if (go_UI_pres_field) uiSync.pressField = go_UI_pres_field.GetComponent<TMP_InputField>();
            if (go_UI_humi_field) uiSync.humidField = go_UI_humi_field.GetComponent<TMP_InputField>();
        }

        // --- 6) ExperimentOrchestrator の参照 ---
        if (orchestrator)
        {
            Undo.RecordObject(orchestrator, "Setup ExperimentOrchestrator");
            orchestrator.spawner = spawner;
            orchestrator.environmentManager = envManager;
            orchestrator.uiSync = uiSync;
        }

        // --- 7) AIRequestSender → Spawner ---
        if (aiSender)
        {
            Undo.RecordObject(aiSender, "Setup AIRequestSender");
            aiSender.spawner = spawner;
        }

        // --- 8) VRExperimentMonitor の参照 ---
        if (vrMonitor && spawner)
        {
            Undo.RecordObject(vrMonitor, "Setup VRExperimentMonitor");
            vrMonitor.spawner = spawner;
            if (go_ExperimentTableZones) vrMonitor.triggerCenter = go_ExperimentTableZones.transform;
            // leftHand / rightHand はプレイヤー依存のためユーザー設定に委ねます
        }

        // --- 9) JsonReactionPlayer の出力UI ---
        if (jsonPlayer && go_UI_HUD_OutputText)
        {
            Undo.RecordObject(jsonPlayer, "Setup JsonReactionPlayer");
            jsonPlayer.output = go_UI_HUD_OutputText.GetComponent<TMPro.TextMeshProUGUI>();
        }

        // --- 10) Start/Reset ボタンが持つ参照（spawner） ---
        if (startBtnComp)
        {
            Undo.RecordObject(startBtnComp, "Setup StartExperimentButton");
            startBtnComp.spawner = spawner;
        }
        if (resetBtnComp)
        {
            Undo.RecordObject(resetBtnComp, "Setup ResetExperimentButton");
            resetBtnComp.spawner = spawner;
        }

        // --- 11) Element/Tool の選択ボタン全自動（直下の子を走査） ---
        if (go_UI_Selectors_Element)
            AutoAssignSelectorButtons(go_UI_Selectors_Element.transform, spawner, "Element");
        if (go_UI_Selectors_Tool)
            AutoAssignSelectorButtons(go_UI_Selectors_Tool.transform, spawner, "Equipment");

        // --- 12) Condition の ± ボタン（ValueAdjustButton）に envManager を差し込む ---
        if (go_UI_Cond_root && envManager)
        {
            var adjusters = go_UI_Cond_root.GetComponentsInChildren<ValueAdjustButton>(true);
            foreach (var a in adjusters)
            {
                Undo.RecordObject(a, "Setup ValueAdjustButton");
                a.envManager = envManager;
                // すでに type / step が設定済みと想定（写真の既存設定を尊重）
            }
        }

        // --- 13) 保存 & 完了 ---
        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(envManager);
        if (uiSync) EditorUtility.SetDirty(uiSync);
        if (orchestrator) EditorUtility.SetDirty(orchestrator);
        if (aiSender) EditorUtility.SetDirty(aiSender);
        if (vrMonitor) EditorUtility.SetDirty(vrMonitor);
        if (jsonPlayer) EditorUtility.SetDirty(jsonPlayer);
        AssetDatabase.SaveAssets();

        Debug.Log("<color=lime>[AutoSetup] 写真のHierarchyに合わせてInspector参照を自動設定しました。</color>");
    }

    // ===== ヘルパー =====
    private static GameObject FindAt(string path)
    {
        var rootScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!rootScene.IsValid()) return null;

        // ルートから順に潜る
        string[] parts = path.Split('/');
        GameObject current = null;

        // ルート候補を探す
        var roots = rootScene.GetRootGameObjects();
        foreach (var r in roots)
        {
            if (r.name == parts[0]) { current = r; break; }
        }
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            var child = current.transform.Find(parts[i]);
            if (!child) return null;
            current = child.gameObject;
        }
        return current;
    }

    private static void AutoAssignSelectorButtons(Transform parent, ChemElementSpawner spawner, string type)
    {
        if (!parent || !spawner) return;

        var stack = new Stack<Transform>();
        stack.Push(parent);

        while (stack.Count > 0)
        {
            var t = stack.Pop();

            var sel = t.GetComponent<SpawnSelectorButton>();
            if (sel)
            {
                Undo.RecordObject(sel, "Setup SpawnSelectorButton");
                sel.spawner = spawner;
                sel.type = type;
                sel.targetName = t.name; // オブジェクト名をそのまま対象名に
                // sel.category は既存設定を尊重（CategoryController連携のため）
                EditorUtility.SetDirty(sel);
            }

            // 子を積む
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }
}
#endif
