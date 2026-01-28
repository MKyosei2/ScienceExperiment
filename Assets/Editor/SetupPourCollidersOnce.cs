// SetupPourCollidersOnce.cs
// Place this file in: Assets/Editor/
//
// Menu: Tools > Chemistry > Setup Pour Colliders (Run Once)
//
// What it does (one-time / idempotent):
// - Finds these paths in ALL loaded scenes:
//   1) BEAKER_EMPTY/PourTarget
//   2) CONICAL_FLASK_H/Spout_H
//   3) CONICAL_FLASK_Cl/Spout_Cl
// - Ensures a BoxCollider exists on each target Transform
// - Sets size/center and IsTrigger=true
// - Will NOT create duplicates (if BoxCollider exists, it adjusts it)

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupPourCollidersOnce
{
    // ====== Adjust these if you want ======
    // PourTarget should be a bit bigger: "receiver" volume
    private static readonly Vector3 PourTargetSize  = new Vector3(0.16f, 0.12f, 0.16f);
    private static readonly Vector3 PourTargetCenter = Vector3.zero;

    // Spout colliders should be smaller: "spout tip" volume
    private static readonly Vector3 SpoutSize  = new Vector3(0.06f, 0.06f, 0.06f);
    private static readonly Vector3 SpoutCenter = Vector3.zero;

    // Safety: if object has lossyScale, BoxCollider size is in local space (works fine),
    // but if you want a minimum size clamp:
    private static readonly Vector3 MinSize = new Vector3(0.02f, 0.02f, 0.02f);

    // =====================================

    [MenuItem("Tools/Chemistry/Setup Pour Colliders (Run Once)", priority = 0)]
    public static void Run()
    {
        int changed = 0;
        int missing = 0;

        changed += EnsureBoxColliderOnPath("BEAKER_EMPTY/PourTarget", PourTargetSize, PourTargetCenter, ref missing);
        changed += EnsureBoxColliderOnPath("CONICAL_FLASK_H/Spout_H", SpoutSize, SpoutCenter, ref missing);
        changed += EnsureBoxColliderOnPath("CONICAL_FLASK_Cl/Spout_Cl", SpoutSize, SpoutCenter, ref missing);

        if (changed > 0)
        {
            // Mark scenes dirty so user remembers to save
            MarkAllLoadedScenesDirty();
        }

        Debug.Log($"[SetupPourCollidersOnce] Done. Changed={changed}, MissingPaths={missing}. (Save your scene)");
    }

    private static int EnsureBoxColliderOnPath(string path, Vector3 size, Vector3 center, ref int missingCount)
    {
        Transform t = FindTransformByPathInLoadedScenes(path);
        if (t == null)
        {
            Debug.LogWarning($"[SetupPourCollidersOnce] NOT FOUND: {path}");
            missingCount++;
            return 0;
        }

        // Ensure BoxCollider
        var bc = t.GetComponent<BoxCollider>();
        if (bc == null)
        {
            Undo.RecordObject(t.gameObject, "Add BoxCollider (SetupPourCollidersOnce)");
            bc = Undo.AddComponent<BoxCollider>(t.gameObject);
        }
        else
        {
            Undo.RecordObject(bc, "Adjust BoxCollider (SetupPourCollidersOnce)");
        }

        // Apply settings
        bc.isTrigger = true;

        // Clamp minimum
        Vector3 s = new Vector3(
            Mathf.Max(size.x, MinSize.x),
            Mathf.Max(size.y, MinSize.y),
            Mathf.Max(size.z, MinSize.z)
        );

        bc.size = s;
        bc.center = center;

        EditorUtility.SetDirty(bc);

        Debug.Log($"[SetupPourCollidersOnce] OK: {path}  -> BoxCollider(size={bc.size}, center={bc.center}, isTrigger={bc.isTrigger})");
        return 1;
    }

    // Finds "RootName/Child/GrandChild" across ALL loaded scenes
    private static Transform FindTransformByPathInLoadedScenes(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return null;

        string[] parts = fullPath.Split('/');
        if (parts.Length == 0) return null;

        for (int si = 0; si < SceneManager.sceneCount; si++)
        {
            Scene sc = SceneManager.GetSceneAt(si);
            if (!sc.isLoaded) continue;

            var roots = sc.GetRootGameObjects();
            for (int ri = 0; ri < roots.Length; ri++)
            {
                var root = roots[ri];
                if (root == null) continue;

                if (root.name != parts[0]) continue;

                Transform cur = root.transform;
                // Transform.Find supports relative path for remaining segments joined by '/'
                if (parts.Length == 1) return cur;

                string rest = string.Join("/", parts, 1, parts.Length - 1);
                Transform found = cur.Find(rest);
                if (found != null) return found;
            }
        }

        return null;
    }

    private static void MarkAllLoadedScenesDirty()
    {
        for (int si = 0; si < SceneManager.sceneCount; si++)
        {
            Scene sc = SceneManager.GetSceneAt(si);
            if (!sc.isLoaded) continue;

            // Mark dirty so Ctrl+S prompts saving
            EditorSceneManager.MarkSceneDirty(sc);
        }
    }
}
#endif
