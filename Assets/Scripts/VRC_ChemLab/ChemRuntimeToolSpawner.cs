using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;

/// <summary>
/// ChemRuntimeToolSpawner (Udon-safe)
/// - Prefabは使わず、Hierarchy上のテンプレ(非Prefab)を VRCInstantiate で複製して生成する。
/// - 検索範囲は toolTemplatesRoot（例: Hierarchy の「Tool」）配下のみ。
/// - 生成した複製は parent の子として配置する。
/// </summary>
public class ChemRuntimeToolSpawner : UdonSharpBehaviour
{
    [Header("Template Root (Hierarchy)")]
    [Tooltip("Hierarchy上に置いたテンプレ群のルート(例: Tool)。この配下の同名オブジェクトを複製して生成します。")]
    public Transform toolTemplatesRoot;

    [Header("Legacy Prefab Spawn (Editor Compatibility)")]
    [Tooltip("旧AutoSetup(Editor)との互換用。現在の実装ではPrefab生成は使用しません。")]
    public bool enablePrefabSpawn = false;
    [Tooltip("旧AutoSetup(Editor)との互換用。現在の実装では使用しません。")]
    public GameObject[] toolPrefabs;
    [Tooltip("旧AutoSetup(Editor)との互換用。現在の実装では使用しません。")]
    public string[] toolIds;


    [Header("Template Visibility")]
    [Tooltip("テンプレ側のRendererを自動で無効化します(複製と見分けが付かない/二重表示を防ぐ)。")]
    public bool hideTemplateRenderers = true;

    [Tooltip("テンプレ GameObject 自体を非アクティブ化します(他システム参照がある場合はOFF推奨)。")]
    public bool deactivateTemplateObject = false;

    [Header("Spawn")]
    [Tooltip("前回生成した複製を破棄してから新規生成します。")]
    public bool destroyPrevious = true;

    [Header("Runtime Safety")]
    [Tooltip("ONにすると、生成した複製からUI/ボタン用のスクリプトを無効化します。\n(生成物を触っただけで同じ場所に複製される現象の対策)")]
    public bool disableSpawnInteractionsOnClones = true;

    private GameObject _spawned;
    private string _spawnedId;

    // =====================================================
    // Multi-instance API (used by ChemElementSpawner)
    // =====================================================
    /// <summary>
    /// Always instantiates a NEW tool instance (does NOT replace/destroy previous ones).
    /// Prefab mode is preferred when toolPrefabs are assigned.
    /// </summary>
    public GameObject InstantiateTool(string toolId)
    {
        string norm = NormalizeId(toolId);
        if (string.IsNullOrEmpty(norm)) return null;

        // Treat prefab mode as enabled when prefabs exist, even if the inspector bool is stale.
        bool usePrefab = (toolPrefabs != null && toolPrefabs.Length > 0);

        // 1) Prefab mode
        if (usePrefab)
        {
            GameObject prefab = GetPrefabById(norm);
            if (prefab != null)
            {
                GameObject go = VRCInstantiate(prefab);
                if (go != null)
                {
                    EnsureCloneVisible(go.transform);
                    if (disableSpawnInteractionsOnClones) DisableRuntimeSpawnInteractions(go);
                    if (!go.activeSelf) go.SetActive(true);
                    return go;
                }
            }
        }

        // 2) Template mode
        Transform templateTr = FindTemplateTransform(norm);
        if (templateTr == null) return null;
        GameObject clone = VRCInstantiate(templateTr.gameObject);
        if (clone == null) return null;
        EnsureCloneVisible(clone.transform);
        if (disableSpawnInteractionsOnClones) DisableRuntimeSpawnInteractions(clone);
        if (!clone.activeSelf) clone.SetActive(true);
        return clone;
    }

    /// <summary>
    /// toolId と同名のテンプレ( toolTemplatesRoot 配下 )を複製して parent に配置する。
    /// </summary>
    public Transform SpawnTool(string toolId, Transform parent, Vector3 worldOffset)
    {
        return SpawnTool(toolId, parent, worldOffset, destroyPrevious);
    }

    /// <summary>
    /// destroyPrev: true の場合は前回生成物を Destroy、false の場合は非表示化。
    /// </summary>
    public Transform SpawnTool(string toolId, Transform parent, Vector3 worldOffset, bool destroyPrev)
    {
        if (parent == null) return null;

        string norm = NormalizeId(toolId);
        if (string.IsNullOrEmpty(norm)) return null;

        // cleanup previous
        if (_spawned != null)
        {
            if (destroyPrev)
            {
                Object.Destroy(_spawned);
            }
            else
            {
                _spawned.SetActive(false);
            }
            _spawned = null;
            _spawnedId = null;
        }        // find template under hierarchy root (UI-safe)
        Transform templateTr = FindTemplateTransform(norm);
        if (templateTr == null)
        {
            Debug.LogWarning("[ChemRuntimeToolSpawner] Tool template not found for id: " + toolId + " (norm=" + norm + ")");
            return null;
        }

        // Instantiate FIRST (so we don't copy disabled renderers into the clone)
        GameObject go = VRCInstantiate(templateTr.gameObject);
        if (go == null) return null;

        // Optionally hide/deactivate the template AFTER cloning
        if (hideTemplateRenderers) DisableAllRenderers(templateTr);
        if (deactivateTemplateObject) templateTr.gameObject.SetActive(false);

        // Parent + placement
        go.transform.SetParent(parent, true);
        go.transform.position = parent.position + worldOffset;

        // Preserve template world-scale under parent (prevents "shape broken" when parent scale != 1)
        Vector3 tScale = templateTr.lossyScale;
        Vector3 pScale = parent.lossyScale;
        Vector3 local = go.transform.localScale;
        local.x = (pScale.x != 0f) ? (tScale.x / pScale.x) : local.x;
        local.y = (pScale.y != 0f) ? (tScale.y / pScale.y) : local.y;
        local.z = (pScale.z != 0f) ? (tScale.z / pScale.z) : local.z;
        go.transform.localScale = local;

        // Ensure clone is visible
        EnableAllRenderers(go.transform);

        if (disableSpawnInteractionsOnClones) DisableRuntimeSpawnInteractions(go);

        if (!go.activeSelf) go.SetActive(true);

        _spawned = go;
        _spawnedId = norm;
        return go.transform;
    }

    // =====================================================
    // Internals
    // =====================================================

    private Transform FindTemplateTransform(string toolIdNorm)
    {
        Transform root = toolTemplatesRoot;

        // Auto-resolve root if not set (UI-safe: never pick Canvas/RectTransform)
        if (root == null)
        {
            // 1) Direct names (scene-level)
            root = ResolveNonUIRootByName("Tool");
            if (root == null) root = ResolveNonUIRootByName("Tools");
            if (root == null) root = ResolveNonUIRootByName("ToolTemplates");

            // 2) Under ExperimentTable (common layout)
            if (root == null)
            {
                GameObject et = GameObject.Find("ExperimentTable");
                if (et != null)
                {
                    root = FindNonUIChildByExactName(et.transform, "Tool");
                    if (root == null) root = FindNonUIChildByExactName(et.transform, "Tools");
                    if (root == null) root = FindNonUIChildByExactName(et.transform, "ToolTemplates");
                }
            }
        }

        if (root == null) return null;

        // Search all descendants (including inactive) because templates may be in categories.
        // IMPORTANT:
        //  - Never pick UI/menu/button objects as "tool templates".
        //  - Prefer actual pick-up tools (VRC_Pickup) when multiple names match.
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        if (all == null) return null;

        Transform best = null;
        int bestScore = -999999;

        int n = all.Length;
        for (int i = 0; i < n; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t == root) continue;

            // Skip any UI-like nodes entirely
            if (IsUiLikeTransform(t)) continue;
            if (t.GetComponent<RectTransform>() != null) continue;
            if (t.GetComponent<Canvas>() != null) continue;

            string name = StripUnitySuffix(t.name);
            string normName = NormalizeId(name);

            bool nameMatch = (normName == toolIdNorm);
            bool baseMatch = false;

            // If template has a Pickup suffix, allow matching the base id (e.g. "BEAKER" matches "Beaker_Pickup")
            if (!nameMatch && normName.EndsWith("PICKUP"))
            {
                string baseName = normName.Substring(0, normName.Length - 6);
                baseMatch = (baseName == toolIdNorm);
            }

            // If requested id includes PICKUP but template doesn't, allow matching as well.
            if (!nameMatch && !baseMatch && toolIdNorm.EndsWith("PICKUP"))
            {
                string reqBase = toolIdNorm.Substring(0, toolIdNorm.Length - 6);
                baseMatch = (normName == reqBase);
            }

            if (!nameMatch && !baseMatch) continue;

            int score = 0;
            // Exact match beats base-match
            if (nameMatch) score += 50;
            if (baseMatch) score += 20;

            // Prefer actual tools (pickup)
            if (t.GetComponent<VRC_Pickup>() != null) score += 100;
            if (t.GetComponent<Rigidbody>() != null) score += 10;
            if (t.GetComponent<Collider>() != null) score += 5;

            // Avoid weird helper sub-objects as templates
            if (t.childCount > 0) score += 2;

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private void DisableAllRenderers(Transform tr)
    {
        if (tr == null) return;
        Renderer[] rs = tr.GetComponentsInChildren<Renderer>(true);
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            rs[i].enabled = false;
        }
    }

    private void EnableAllRenderers(Transform tr)
    {
        if (tr == null) return;
        Renderer[] rs = tr.GetComponentsInChildren<Renderer>(true);
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            rs[i].enabled = true;
        }
    }

    private bool IsLikelyUIRoot(Transform tr)
    {
        if (tr == null) return false;
        if (tr.GetComponent<RectTransform>() != null) return true;
        if (tr.GetComponent<Canvas>() != null) return true;
        string n = tr.name;
        if (string.IsNullOrEmpty(n)) return false;
        n = n.ToUpper();
        if (n.Contains("CANVAS") || n.Contains("UI") || n.Contains("PANEL") || n.Contains("BUTTON")) return true;
        return false;
    }

    private bool HasAnyRenderer(Transform tr)
    {
        if (tr == null) return false;
        Renderer r = tr.GetComponent<Renderer>();
        if (r != null) return true;
        Renderer[] rs = tr.GetComponentsInChildren<Renderer>(true);
        return rs != null && rs.Length > 0;
    }

    private Transform ResolveNonUIRootByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        GameObject g = GameObject.Find(name);
        if (g == null) return null;
        Transform t = g.transform;
        if (IsLikelyUIRoot(t)) return null;
        if (!HasAnyRenderer(t)) return null;
        return t;
    }

    private Transform FindNonUIChildByExactName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName)) return null;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        if (all == null) return null;
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t.name != exactName) continue;
            if (IsLikelyUIRoot(t)) continue;
            if (!HasAnyRenderer(t)) continue;
            return t;
        }
        return null;
    }

    private string StripUnitySuffix(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim();

        // Remove Unity duplicate suffix like " (1)" or " (Clone)"
        int p = s.LastIndexOf('(');
        if (p > 0 && s.EndsWith(")"))
        {
            // Ensure it's a trailing suffix with a preceding space
            if (s[p - 1] == ' ')
            {
                s = s.Substring(0, p - 1).Trim();
            }
        }
        return s;
    }

    private string NormalizeId(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = StripUnitySuffix(s).Trim();
        s = s.Replace(" ", "");
        s = s.Replace("_", "");
        s = s.Replace("-", "");
        return s.ToUpper();
    }

    // =====================================================
    // Prefab helpers (Udon-safe)
    // =====================================================
    private GameObject GetPrefabById(string toolIdNorm)
    {
        if (toolPrefabs == null || toolPrefabs.Length == 0) return null;

        // If toolIds is available, use it.
        if (toolIds != null && toolIds.Length == toolPrefabs.Length)
        {
            int n = toolIds.Length;
            for (int i = 0; i < n; i++)
            {
                string id = toolIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (NormalizeId(id) == toolIdNorm)
                    return toolPrefabs[i];
            }
        }

        // Fallback: match prefab.name
        int m = toolPrefabs.Length;
        for (int i = 0; i < m; i++)
        {
            GameObject p = toolPrefabs[i];
            if (p == null) continue;
            if (NormalizeId(p.name) == toolIdNorm) return p;
        }
        return null;
    }

    /// <summary>
    /// Runtimeで生成した複製が、UIボタンのように振る舞ってしまうのを防ぎます。
    /// (例: SpawnSelectorButton / SelectorObject が残っていて、触っただけでその場で再生成される)
    /// </summary>
    private void DisableRuntimeSpawnInteractions(GameObject root)
    {
        if (root == null) return;

        // -----------------------------------------------------------------
        // IMPORTANT:
        // In VRChat, Interact events can still be routed even if a behaviour
        // is disabled. So we do BOTH:
        //  1) disable the behaviour
        //  2) clear its references / ids so it becomes inert
        // -----------------------------------------------------------------

        // SpawnSelectorButton (element/tool/condition selector)
        SpawnSelectorButton[] spawnBtns = root.GetComponentsInChildren<SpawnSelectorButton>(true);
        for (int i = 0; i < spawnBtns.Length; i++)
        {
            SpawnSelectorButton b = spawnBtns[i];
            if (b == null) continue;
            b.idOrName = "";
            b.elementSpawner = null;
            b.environmentManager = null;
            b.statusDisplay = null;
            b.enabled = false;
        }

        // SelectorObject (adds selection into SelectedObjectHolder)
        SelectorObject[] selectorObjs = root.GetComponentsInChildren<SelectorObject>(true);
        for (int i = 0; i < selectorObjs.Length; i++)
        {
            SelectorObject s = selectorObjs[i];
            if (s == null) continue;
            s.selected = null;
            s.zoneForThisCategory = null;
            s.idOverride = "";
            s.enabled = false;
        }

        // SelectionActionController (calls button.Interact())
        SelectionActionController[] actionCtrls = root.GetComponentsInChildren<SelectionActionController>(true);
        for (int i = 0; i < actionCtrls.Length; i++)
        {
            SelectionActionController a = actionCtrls[i];
            if (a == null) continue;
            a.buttons = null;
            a.enabled = false;
        }

        // ValueAdjustButton (env.Modify())
        ValueAdjustButton[] valueBtns = root.GetComponentsInChildren<ValueAdjustButton>(true);
        for (int i = 0; i < valueBtns.Length; i++)
        {
            ValueAdjustButton v = valueBtns[i];
            if (v == null) continue;
            v.env = null;
            v.command = "";
            v.enabled = false;
        }

        // Start/Reset/Operator buttons
        StartExperimentButton[] startBtns = root.GetComponentsInChildren<StartExperimentButton>(true);
        for (int i = 0; i < startBtns.Length; i++)
        {
            StartExperimentButton b = startBtns[i];
            if (b == null) continue;
            b.spawner = null;
            b.enabled = false;
        }

        ResetExperimentButton[] resetBtns = root.GetComponentsInChildren<ResetExperimentButton>(true);
        for (int i = 0; i < resetBtns.Length; i++)
        {
            ResetExperimentButton b = resetBtns[i];
            if (b == null) continue;
            b.spawner = null;
            b.enabled = false;
        }

        OperatorButton[] opBtns = root.GetComponentsInChildren<OperatorButton>(true);
        for (int i = 0; i < opBtns.Length; i++)
        {
            OperatorButton b = opBtns[i];
            if (b == null) continue;
            b.spawner = null;
            b.mode = "";
            b.enabled = false;
        }

        // ConditionAdjuster (UI side)
        ConditionAdjuster[] condAdj = root.GetComponentsInChildren<ConditionAdjuster>(true);
        for (int i = 0; i < condAdj.Length; i++)
        {
            ConditionAdjuster c = condAdj[i];
            if (c == null) continue;
            c.env = null;
            c.spawner = null;
            c.command = "";
            c.enabled = false;
        }

    
        // Build/runtime fallback:
        // In Udon runtime, UI button scripts can appear as plain UdonBehaviours.
        // Disable any UdonBehaviours/Colliders that live under UI-like transforms.
        DisableUiLikeUdonBehaviours(root);

    }

    private void DisableUiLikeUdonBehaviours(GameObject root)
    {
        if (root == null) return;

        UdonBehaviour[] ubs = root.GetComponentsInChildren<UdonBehaviour>(true);
        if (ubs != null)
        {
            for (int i = 0; i < ubs.Length; i++)
            {
                UdonBehaviour ub = ubs[i];
                if (ub == null) continue;
                if (!IsUiLikeTransform(ub.transform)) continue;
                ub.enabled = false;
            }
        }

        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        if (cols != null)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null) continue;
                if (!IsUiLikeTransform(col.transform)) continue;
                col.enabled = false;
            }
        }
    }

    private bool IsUiLikeTransform(Transform t)
    {
        int guard = 0;
        while (t != null && guard < 32)
        {
            string n = t.name;
            if (!string.IsNullOrEmpty(n))
            {
                if (n.Contains("Button") || n.Contains("Buttons") || n.Contains("UI") || n.Contains("Selector") || n.Contains("Panel") || n.Contains("Canvas") || n.Contains("Menu"))
                {
                    return true;
                }
            }
            t = t.parent;
            guard++;
        }
        return false;
    }

    private void EnsureCloneVisible(Transform root)
    {
        if (root == null) return;
        // Make sure renderers are enabled (some prefabs ship disabled)
        EnableAllRenderers(root);

        // Make sure colliders are enabled too
        Collider[] cs = root.GetComponentsInChildren<Collider>(true);
        if (cs != null)
        {
            for (int i = 0; i < cs.Length; i++)
            {
                if (cs[i] == null) continue;
                cs[i].enabled = true;
            }
        }
    }
}
