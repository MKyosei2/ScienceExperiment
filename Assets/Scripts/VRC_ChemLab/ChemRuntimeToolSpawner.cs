using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

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

    private GameObject _spawned;
    private string _spawnedId;

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
        }

        // find template under hierarchy root
        Transform templateTr = FindTemplateTransform(norm);
        if (templateTr == null)
        {
            // NOTE: toolIdNorm is scoped inside FindTemplateTransform; log the normalized id here.
            Debug.LogWarning("[ChemRuntimeToolSpawner] Tool template not found for id: " + toolId + " (norm=" + norm + ")");
            return null;
        }

        if (hideTemplateRenderers) DisableAllRenderers(templateTr);
        if (deactivateTemplateObject) templateTr.gameObject.SetActive(false);

        GameObject go = VRCInstantiate(templateTr.gameObject);
        if (go == null) return null;

        go.transform.SetParent(parent, true);
        go.transform.position = parent.position + worldOffset;
        go.transform.rotation = parent.rotation;
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

        // Auto-resolve root if not set (prefer "Tool" / "Tools" / "ToolTemplates")
        if (root == null)
        {
            GameObject g = GameObject.Find("Tool");
            if (g == null) g = GameObject.Find("Tools");
            if (g == null) g = GameObject.Find("ToolTemplates");
            if (g != null) root = g.transform;
        }

        if (root == null) return null;

        // Search all descendants (including inactive) because templates may be in categories.
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        if (all == null) return null;

        int n = all.Length;
        for (int i = 0; i < n; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            if (t == root) continue;

            string name = StripUnitySuffix(t.name);
            string normName = NormalizeId(name);
            if (normName == toolIdNorm) return t;

            // If template has a Pickup suffix, allow matching the base id (e.g. "BEAKER" matches "Beaker_Pickup")
            if (normName.EndsWith("PICKUP"))
            {
                string baseName = normName.Substring(0, normName.Length - 6);
                if (baseName == toolIdNorm) return t;
            }

            // If requested id includes PICKUP but template doesn't, allow matching as well.
            if (toolIdNorm.EndsWith("PICKUP"))
            {
                string reqBase = toolIdNorm.Substring(0, toolIdNorm.Length - 6);
                if (normName == reqBase) return t;
            }

            // Some projects use PICKUP capitalization variations already handled by NormalizeId// Some projects use "_PICKUP" capitalization variations already handled by NormalizeId
        }

        return null;
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
}