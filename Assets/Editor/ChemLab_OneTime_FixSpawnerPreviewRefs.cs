// Auto-generated fix: removes dependency on SelectionCategory. Udon-safe (Editor-only).
// Place this file at: Assets/Editor/ChemLab_OneTime_FixSpawnerPreviewRefs.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChemLab_OneTime_FixSpawnerPreviewRefs
{
    private const string MenuPath = "Tools/ChemLab/OneTime/Fix Spawner Inspector (No Tags / No Enums)";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        int spawnerCount = 0;
        int fixedToolRoot = 0;
        int fixedAnchor = 0;
        int fixedSampleVisual = 0;

        // Find all ChemElementSpawner instances in loaded scenes (including inactive).
        var spawners = FindObjectsByTypeName("ChemElementSpawner");
        if (spawners.Count == 0)
        {
            Debug.LogError("[ChemLab] No ChemElementSpawner found in loaded scenes.");
            return;
        }

        foreach (var obj in spawners)
        {
            var spawner = obj as Component;
            if (spawner == null) continue;
            spawnerCount++;

            var so = new SerializedObject(spawner);

            // 1) Fix toolModelsRoot: avoid UI objects; point to a props root with renderers.
            var toolModelsRootProp = so.FindProperty("toolModelsRoot");
            Transform toolModelsRoot = toolModelsRootProp != null ? toolModelsRootProp.objectReferenceValue as Transform : null;

            bool needsToolRoot = toolModelsRoot == null || IsLikelyUIOrButton(toolModelsRoot.gameObject);
            if (needsToolRoot)
            {
                Transform propsRoot = FindBestPropsRoot();
                if (propsRoot != null && (toolModelsRoot == null || toolModelsRoot != propsRoot))
                {
                    if (toolModelsRootProp != null) toolModelsRootProp.objectReferenceValue = propsRoot;
                    fixedToolRoot++;
                }
            }

            // 2) Ensure containerTransform: if missing, use toolModelsRoot or spawner transform
            var containerProp = so.FindProperty("containerTransform");
            if (containerProp != null && containerProp.objectReferenceValue == null)
            {
                Transform fallback = (toolModelsRootProp != null ? toolModelsRootProp.objectReferenceValue as Transform : null) ?? spawner.transform;
                containerProp.objectReferenceValue = fallback;
            }

            // 3) Ensure elementEffectAnchorFallback exists
            var anchorProp = so.FindProperty("elementEffectAnchorFallback");
            if (anchorProp != null && anchorProp.objectReferenceValue == null)
            {
                Transform container = (containerProp != null ? containerProp.objectReferenceValue as Transform : null) ?? spawner.transform;
                if (container != null)
                {
                    Transform anchor = container.Find("ElementEffectAnchor");
                    if (anchor == null)
                    {
                        var go = new GameObject("ElementEffectAnchor");
                        go.transform.SetParent(container, false);
                        anchor = go.transform;
                        fixedAnchor++;
                    }
                    anchorProp.objectReferenceValue = anchor;
                }
            }

            // 4) Turn on common preview flags if they exist (by name)
            SetBoolIfExists(so, "previewToolOnSelect", true);
            SetBoolIfExists(so, "placeElementEffectInTool", true);
            SetBoolIfExists(so, "previewElementOnSelect", true);
            SetBoolIfExists(so, "spawnToolOnSelect", true);

            // 5) Fix sampleVisual if missing (best-effort by type name)
            var sampleVisualProp = so.FindProperty("sampleVisual");
            if (sampleVisualProp != null && sampleVisualProp.objectReferenceValue == null)
            {
                var chemVisualController = FindFirstObjectByTypeName("ChemVisualController");
                if (chemVisualController != null)
                {
                    sampleVisualProp.objectReferenceValue = chemVisualController;
                    fixedSampleVisual++;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[ChemLab] Fix complete. Spawners: {spawnerCount}, toolModelsRoot fixed: {fixedToolRoot}, anchors created: {fixedAnchor}, sampleVisual fixed: {fixedSampleVisual}");
    }

    private static void SetBoolIfExists(SerializedObject so, string propName, bool value)
    {
        var p = so.FindProperty(propName);
        if (p != null && p.propertyType == SerializedPropertyType.Boolean)
        {
            p.boolValue = value;
        }
    }

    /// <summary>
    /// Heuristic: treat objects as UI/button if they have UI components or are on typical UI layers.
    /// No Tag usage.
    /// </summary>
    private static bool IsLikelyUIOrButton(GameObject go)
    {
        if (go == null) return false;

        // Common UI layers: 5 ("UI") and project-specific (e.g., 13) used by your buttons.
        if (go.layer == 5 || go.layer == 13) return true;

        // UI components
        if (go.GetComponent<RectTransform>() != null) return true;

        // Unity UI modules may not be installed, so use reflection for Selectable/Canvas/Graphic
        if (HasComponentByTypeName(go, "Canvas")) return true;
        if (HasComponentByTypeName(go, "CanvasRenderer")) return true;
        if (HasComponentByTypeName(go, "UnityEngine.UI.Selectable")) return true;
        if (HasComponentByTypeName(go, "UnityEngine.UI.Graphic")) return true;

        // Common naming patterns
        string n = go.name.ToLowerInvariant();
        if (n.Contains("button") || n.Contains("ui") || n.Contains("canvas") || n.Contains("panel") || n.Contains("selector"))
            return true;

        return false;
    }

    private static bool HasComponentByTypeName(GameObject go, string typeName)
    {
        var t = ResolveType(typeName);
        if (t == null) return false;
        return go.GetComponent(t) != null;
    }

    /// <summary>
    /// Find the best candidate root for tool models: prefers objects with many Renderers and few/no UI components.
    /// </summary>
    private static Transform FindBestPropsRoot()
    {
        // Try common names first
        var preferredNames = new[]
        {
            "VR_Props", "VRProps", "VR Props", "Props", "LabProps", "Tools", "ToolModels", "Equipment", "EquipmentModels"
        };

        foreach (var name in preferredNames)
        {
            var go = GameObject.Find(name);
            if (go != null && !IsLikelyUIOrButton(go))
                return go.transform;
        }

        // Fallback: scan all loaded scene roots, choose object that maximizes renderer count.
        Transform best = null;
        int bestScore = int.MinValue;

        foreach (var root in GetAllSceneRootObjects())
        {
            if (root == null) continue;
            if (IsLikelyUIOrButton(root)) continue;

            // Skip obvious managers/system roots
            string rn = root.name.ToLowerInvariant();
            if (rn.Contains("ui") || rn.Contains("canvas") || rn.Contains("system") || rn.Contains("manager")) continue;

            int renderers = CountComponentsInHierarchy(root.transform, "Renderer");
            if (renderers < 2) continue; // props root should have at least a couple renderers

            int uiComps = CountUIInHierarchy(root.transform);
            int score = renderers * 10 - uiComps * 50; // strongly penalize UI

            if (score > bestScore)
            {
                best = root.transform;
                bestScore = score;
            }
        }

        return best;
    }

    private static int CountUIInHierarchy(Transform t)
    {
        if (t == null) return 0;
        int count = 0;
        var stack = new Stack<Transform>();
        stack.Push(t);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur == null) continue;
            var go = cur.gameObject;

            if (go.layer == 5 || go.layer == 13) count++;
            if (go.GetComponent<RectTransform>() != null) count++;
            if (HasComponentByTypeName(go, "Canvas")) count++;
            if (HasComponentByTypeName(go, "UnityEngine.UI.Selectable")) count++;

            for (int i = 0; i < cur.childCount; i++)
                stack.Push(cur.GetChild(i));
        }
        return count;
    }

    private static int CountComponentsInHierarchy(Transform t, string typeName)
    {
        var compType = ResolveType(typeName) ?? ResolveType("UnityEngine." + typeName);
        if (compType == null) return 0;

        int count = 0;
        var stack = new Stack<Transform>();
        stack.Push(t);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur == null) continue;

            if (cur.GetComponent(compType) != null) count++;

            for (int i = 0; i < cur.childCount; i++)
                stack.Push(cur.GetChild(i));
        }
        return count;
    }

    private static List<UnityEngine.Object> FindObjectsByTypeName(string typeName)
    {
        var t = ResolveType(typeName);
        if (t == null) return new List<UnityEngine.Object>();

        // Editor-only: can find inactive and assets. Filter to scene objects.
        var all = Resources.FindObjectsOfTypeAll(t);
        return all.Where(o => o is Component c && c.gameObject.scene.IsValid()).ToList();
    }

    private static UnityEngine.Object FindFirstObjectByTypeName(string typeName)
    {
        var list = FindObjectsByTypeName(typeName);
        return list.Count > 0 ? list[0] : null;
    }

    private static IEnumerable<GameObject> GetAllSceneRootObjects()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                yield return root;
        }
    }

    private static Type ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        // Fully qualified?
        var t = Type.GetType(typeName);
        if (t != null) return t;

        // Try UnityEngine prefix
        t = Type.GetType("UnityEngine." + typeName + ", UnityEngine");
        if (t != null) return t;

        // Search all assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var tt = asm.GetTypes().FirstOrDefault(x => x.Name == typeName || x.FullName == typeName);
                if (tt != null) return tt;
            }
            catch { /* ignore */ }
        }
        return null;
    }
}
#endif
