#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ChemLab_OneTime_ValidateFixSpawnerInspector
{
    [MenuItem("Tools/ChemLab/OneTime/Validate & Fix ChemElementSpawner Inspector (3D Preview)")]
    public static void Run()
    {
        int fixedCount = 0;
        int total = 0;

        // Editor-only: safe to use Resources.FindObjectsOfTypeAll to include inactive / prefab stage objects.
        var spawners = Resources.FindObjectsOfTypeAll<ChemElementSpawner>();
        if (spawners == null || spawners.Length == 0)
        {
            Debug.Log("[ChemLab] ChemElementSpawner not found in open scenes.");
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;
            // Skip assets/prefabs not in scene
            if (!spawner.gameObject.scene.IsValid()) continue;

            total++;

            bool changed = false;

            // Force the intended behavior
            if (!spawner.placeElementEffectInTool) { spawner.placeElementEffectInTool = true; changed = true; }
            if (!spawner.previewToolOnSelect) { spawner.previewToolOnSelect = true; changed = true; }

            // containerTransform fallback
            if (spawner.containerTransform == null) { spawner.containerTransform = spawner.transform; changed = true; }

            // elementEffectAnchorFallback fallback
            if (spawner.elementEffectAnchorFallback == null) { spawner.elementEffectAnchorFallback = spawner.containerTransform; changed = true; }

            // toolModelsRoot: must point to 3D props root (NOT UI)
            if (spawner.toolModelsRoot == null || IsLikelyUIRoot(spawner.toolModelsRoot))
            {
                var found = FindToolModelsRootHeuristic(spawner.transform.root);
                if (found != null && !IsLikelyUIRoot(found))
                {
                    spawner.toolModelsRoot = found;
                    changed = true;
                }
            }

            if (spawner.sampleVisual == null)
            {
                Debug.LogWarning($"[ChemLab] sampleVisual is NOT set on ChemElementSpawner '{spawner.name}'. Element 3D will not show until set.");
            }

            if (spawner.toolModelsRoot == null)
            {
                Debug.LogWarning($"[ChemLab] toolModelsRoot is NOT set (or points to UI) on ChemElementSpawner '{spawner.name}'. Tool 3D will not show until set.");
            }

            if (changed)
            {
                fixedCount++;
                EditorUtility.SetDirty(spawner);
            }
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[ChemLab] Validate/Fix done. Fixed {fixedCount}/{total} ChemElementSpawner(s). Save the scene(s).");
        }
        else
        {
            Debug.Log($"[ChemLab] Validate/Fix done. No changes needed. Checked {total} ChemElementSpawner(s).");
        }
    }

    private static bool IsLikelyUIRoot(Transform tr)
    {
        if (tr == null) return false;
        if (tr.GetComponent<RectTransform>() != null) return true;
        if (tr.GetComponent<Canvas>() != null) return true;

        var n = tr.name;
        if (string.IsNullOrEmpty(n)) return false;
        n = n.ToUpperInvariant();
        return n.Contains("CANVAS") || n.Contains("UI") || n.Contains("BUTTON") || n.Contains("PANEL");
    }

    private static Transform FindToolModelsRootHeuristic(Transform sceneRoot)
    {
        if (sceneRoot == null) return null;

        // 1) Try common names first
        string[] names = { "VR_Props", "VRProps", "VR Props", "Props", "ToolModels", "Tools3D", "Equipments", "Equipment" };
        foreach (var nm in names)
        {
            var go = GameObject.Find(nm);
            if (go != null) return go.transform;
        }

        // 2) Limited heuristic: pick a plausible root (by name) with most Renderers under it
        Transform best = null;
        int bestCount = 0;

        int c0 = sceneRoot.childCount;
        for (int i = 0; i < c0; i++)
        {
            var ch0 = sceneRoot.GetChild(i);
            ConsiderCandidate(ch0, ref best, ref bestCount);

            int c1 = ch0.childCount;
            for (int j = 0; j < c1; j++)
            {
                var ch1 = ch0.GetChild(j);
                ConsiderCandidate(ch1, ref best, ref bestCount);
            }
        }

        return best;
    }

    private static void ConsiderCandidate(Transform tr, ref Transform best, ref int bestCount)
    {
        if (tr == null) return;
        if (IsLikelyUIRoot(tr)) return;

        var n = tr.name;
        if (string.IsNullOrEmpty(n)) return;
        var up = n.ToUpperInvariant();

        bool plausible = up.Contains("PROP") || up.Contains("TOOL") || up.Contains("EQUIP") || up.Contains("MODEL") || up.Contains("VR");
        if (!plausible) return;

        int count = CountRenderersUnder(tr, 5, 3000);
        if (count > bestCount)
        {
            bestCount = count;
            best = tr;
        }
    }

    private static int CountRenderersUnder(Transform tr, int maxDepth, int maxNodes)
    {
        int visited = 0;
        return CountRec(tr, maxDepth, maxNodes, ref visited);
    }

    private static int CountRec(Transform tr, int depthLeft, int maxNodes, ref int visited)
    {
        if (tr == null) return 0;
        if (visited >= maxNodes) return 0;
        visited++;

        int count = (tr.GetComponent<Renderer>() != null) ? 1 : 0;
        if (depthLeft <= 0) return count;

        int c = tr.childCount;
        for (int i = 0; i < c; i++)
            count += CountRec(tr.GetChild(i), depthLeft - 1, maxNodes, ref visited);

        return count;
    }
}
#endif
