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
//
// Notes:
// - If the target Transform has no Renderer, it will use parent's Renderer bounds to estimate size/placement.
// - This is for "pour hit" volumes only (not for grabbing/physics).

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupPourCollidersOnce
{
    // =====================================
    // TUNING (edit here if needed)
    // =====================================

    // If true, sizes are computed from Renderer bounds where possible.
    // If false, uses the fixed sizes below.
    private const bool UseAutoSizingFromBounds = true;

    // Overall multipliers for auto sizing
    // Receiver (PourTarget) should be "easier to hit"
    private const float ReceiverXY_Mult = 0.55f; // relative to parent extents (x/z)
    private const float ReceiverY_Mult  = 0.20f; // relative to parent extents (y)

    // Spout should be smaller than receiver
    private const float SpoutXY_Mult = 0.18f;    // relative to parent extents (x/z)
    private const float SpoutY_Mult  = 0.18f;    // relative to parent extents (y)

    // Minimum / Maximum size clamps (meters)
    private static readonly Vector3 ReceiverMinSize = new Vector3(0.10f, 0.06f, 0.10f);
    private static readonly Vector3 ReceiverMaxSize = new Vector3(0.28f, 0.18f, 0.28f);

    private static readonly Vector3 SpoutMinSize = new Vector3(0.03f, 0.03f, 0.03f);
    private static readonly Vector3 SpoutMaxSize = new Vector3(0.12f, 0.12f, 0.12f);

    // Fallback fixed sizes if UseAutoSizingFromBounds == false
    private static readonly Vector3 FixedReceiverSize = new Vector3(0.16f, 0.12f, 0.16f);
    private static readonly Vector3 FixedSpoutSize    = new Vector3(0.06f, 0.06f, 0.06f);

    // Where to place the receiver center relative to parent's top (world-space), if we can estimate.
    // Positive means "below the top surface".
    private const float ReceiverTopDownOffset = 0.015f; // 1.5cm ‰º

    // Optional: inflate bounds slightly so intersects is easier
    private const float BoundsInflate = 0.00f; // 0.00`0.02‚­‚ç‚¢‚Ü‚Å

    // =====================================

    [MenuItem("Tools/Chemistry/Setup Pour Colliders (Run Once)", priority = 0)]
    public static void Run()
    {
        int changed = 0;
        int missing = 0;

        changed += EnsureBoxColliderOnPath(
            "BEAKER_EMPTY/PourTarget",
            role: TargetRole.Receiver,
            ref missing
        );

        changed += EnsureBoxColliderOnPath(
            "CONICAL_FLASK_H/Spout_H",
            role: TargetRole.Spout,
            ref missing
        );

        changed += EnsureBoxColliderOnPath(
            "CONICAL_FLASK_Cl/Spout_Cl",
            role: TargetRole.Spout,
            ref missing
        );

        if (changed > 0)
        {
            MarkAllLoadedScenesDirty();
        }

        Debug.Log($"[SetupPourCollidersOnce] Done. Changed={changed}, MissingPaths={missing}. (Save your scene)");
    }

    private enum TargetRole
    {
        Receiver, // PourTarget
        Spout     // Spout_H / Spout_Cl
    }

    private static int EnsureBoxColliderOnPath(string path, TargetRole role, ref int missingCount)
    {
        Transform t = FindTransformByPathInLoadedScenes(path);
        if (t == null)
        {
            Debug.LogWarning($"[SetupPourCollidersOnce] NOT FOUND: {path}");
            missingCount++;
            return 0;
        }

        // Ensure BoxCollider
        BoxCollider bc = t.GetComponent<BoxCollider>();
        if (bc == null)
        {
            Undo.RecordObject(t.gameObject, "Add BoxCollider (SetupPourCollidersOnce)");
            bc = Undo.AddComponent<BoxCollider>(t.gameObject);
        }
        else
        {
            Undo.RecordObject(bc, "Adjust BoxCollider (SetupPourCollidersOnce)");
        }

        bc.isTrigger = true;

        // Decide size/center
        Vector3 sizeLocal;
        Vector3 centerLocal;

        if (UseAutoSizingFromBounds)
        {
            ComputeAutoBoxForRole(t, role, out sizeLocal, out centerLocal);
        }
        else
        {
            sizeLocal = (role == TargetRole.Receiver) ? FixedReceiverSize : FixedSpoutSize;
            centerLocal = Vector3.zero;
        }

        // Apply
        bc.size = sizeLocal;
        bc.center = centerLocal;

        EditorUtility.SetDirty(bc);

        Debug.Log($"[SetupPourCollidersOnce] OK: {path} -> BoxCollider(size={bc.size}, center={bc.center}, isTrigger={bc.isTrigger})");
        return 1;
    }

    private static void ComputeAutoBoxForRole(Transform target, TargetRole role, out Vector3 sizeLocal, out Vector3 centerLocal)
    {
        // Default
        sizeLocal = (role == TargetRole.Receiver) ? FixedReceiverSize : FixedSpoutSize;
        centerLocal = Vector3.zero;

        // 1) Try bounds from target (renderer under it)
        Bounds? targetBoundsWorld = TryGetRendererBoundsWorld(target);

        // 2) If no renderer, try parent's bounds (beaker/flask body)
        Bounds? parentBoundsWorld = null;
        if (!targetBoundsWorld.HasValue)
        {
            if (target.parent != null) parentBoundsWorld = TryGetRendererBoundsWorld(target.parent);
        }

        // Choose a reference bounds (prefer parent for receiver, target for spout if available)
        Bounds? refBoundsWorld = null;
        if (role == TargetRole.Receiver)
        {
            refBoundsWorld = parentBoundsWorld.HasValue ? parentBoundsWorld : targetBoundsWorld;
        }
        else
        {
            refBoundsWorld = targetBoundsWorld.HasValue ? targetBoundsWorld : parentBoundsWorld;
        }

        if (!refBoundsWorld.HasValue)
        {
            // Can't estimate; use fixed.
            sizeLocal = ClampSize(sizeLocal, role);
            centerLocal = Vector3.zero;
            return;
        }

        Bounds b = refBoundsWorld.Value;
        if (BoundsInflate > 0f) b.Expand(BoundsInflate * 2f);

        // Compute world size proposal
        Vector3 worldSize;

        if (role == TargetRole.Receiver)
        {
            // Receiver = beaker mouth volume (bigger)
            // Use XZ from bounds extents, Y smaller
            float sx = Mathf.Max(ReceiverMinSize.x, Mathf.Min(ReceiverMaxSize.x, b.extents.x * 2f * ReceiverXY_Mult));
            float sz = Mathf.Max(ReceiverMinSize.z, Mathf.Min(ReceiverMaxSize.z, b.extents.z * 2f * ReceiverXY_Mult));
            float sy = Mathf.Max(ReceiverMinSize.y, Mathf.Min(ReceiverMaxSize.y, b.extents.y * 2f * ReceiverY_Mult));
            worldSize = new Vector3(sx, sy, sz);

            // Place receiver around "top center - offset"
            Vector3 topCenterWorld = new Vector3(b.center.x, b.max.y - ReceiverTopDownOffset, b.center.z);
            centerLocal = target.InverseTransformPoint(topCenterWorld);
        }
        else
        {
            // Spout = small box
            float sx = Mathf.Max(SpoutMinSize.x, Mathf.Min(SpoutMaxSize.x, b.extents.x * 2f * SpoutXY_Mult));
            float sz = Mathf.Max(SpoutMinSize.z, Mathf.Min(SpoutMaxSize.z, b.extents.z * 2f * SpoutXY_Mult));
            float sy = Mathf.Max(SpoutMinSize.y, Mathf.Min(SpoutMaxSize.y, b.extents.y * 2f * SpoutY_Mult));
            worldSize = new Vector3(sx, sy, sz);

            // Center at target origin unless we can use renderer bounds directly
            // If target has its own renderer bounds, center to that bounds center
            if (targetBoundsWorld.HasValue)
            {
                centerLocal = target.InverseTransformPoint(targetBoundsWorld.Value.center);
            }
            else
            {
                centerLocal = Vector3.zero;
            }
        }

        // Convert world size to local size approximately.
        // BoxCollider.size is in local space, so we divide by lossyScale magnitude per axis.
        Vector3 ls = target.lossyScale;
        ls.x = Mathf.Abs(ls.x) < 0.0001f ? 1f : Mathf.Abs(ls.x);
        ls.y = Mathf.Abs(ls.y) < 0.0001f ? 1f : Mathf.Abs(ls.y);
        ls.z = Mathf.Abs(ls.z) < 0.0001f ? 1f : Mathf.Abs(ls.z);

        sizeLocal = new Vector3(worldSize.x / ls.x, worldSize.y / ls.y, worldSize.z / ls.z);
        sizeLocal = ClampSize(sizeLocal, role);
    }

    private static Vector3 ClampSize(Vector3 sizeLocal, TargetRole role)
    {
        // Clamp in local-size space (good enough)
        Vector3 min = (role == TargetRole.Receiver) ? ReceiverMinSize : SpoutMinSize;
        Vector3 max = (role == TargetRole.Receiver) ? ReceiverMaxSize : SpoutMaxSize;

        // Note: min/max are "world intended" but we clamp here as local.
        // If scaling is huge, the effective world size might differ; still better than tiny colliders.
        sizeLocal.x = Mathf.Clamp(sizeLocal.x, min.x, max.x);
        sizeLocal.y = Mathf.Clamp(sizeLocal.y, min.y, max.y);
        sizeLocal.z = Mathf.Clamp(sizeLocal.z, min.z, max.z);

        return sizeLocal;
    }

    private static Bounds? TryGetRendererBoundsWorld(Transform root)
    {
        if (root == null) return null;

        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs == null || rs.Length == 0) return null;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            b.Encapsulate(rs[i].bounds);
        }
        return b;
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

            GameObject[] roots = sc.GetRootGameObjects();
            for (int ri = 0; ri < roots.Length; ri++)
            {
                GameObject root = roots[ri];
                if (root == null) continue;

                if (root.name != parts[0]) continue;

                Transform cur = root.transform;

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

            EditorSceneManager.MarkSceneDirty(sc);
        }
    }
}
#endif
