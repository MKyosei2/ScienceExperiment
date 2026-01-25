using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClampElementVfxInsideConicalFlask_Once
{
    private const string FLASK_H_NAME = "CONICAL_FLASK_H";
    private const string FLASK_CL_NAME = "CONICAL_FLASK_Cl";

    private const string ANCHOR_NAME = "ElementEffectAnchor";
    private const string VOLUME_NAME = "VFXVolumeMesh";

    // ★ここが効きます：小さくするほど“絶対にあふれない”
    // 0.45: かなり安全 / 0.35: もっと安全 / 0.25: ほぼ確実に内側だけ
    private const float SAFE_INNER_SCALE = 0.35f;

    // 少し下に寄せる（首からはみ出すのを防ぐ）
    private const float Y_OFFSET = -0.01f;

    [MenuItem("ChemLab/Clamp Element VFX Inside CONICAL_FLASK (Force Safe) (Once)")]
    public static void Run()
    {
        var h = GameObject.Find(FLASK_H_NAME);
        var cl = GameObject.Find(FLASK_CL_NAME);

        if (h == null || cl == null)
        {
            Debug.LogError(
                "対象フラスコが見つかりません。\n" +
                $"{FLASK_H_NAME}: {(h ? "OK" : "NOT FOUND")}\n" +
                $"{FLASK_CL_NAME}: {(cl ? "OK" : "NOT FOUND")}"
            );
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clamp Element VFX Inside Conical Flask");

        ClampOne(h);
        ClampOne(cl);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("✅ 完了：ElementVisual をフラスコ内に確実に収まるサイズへ強制縮小しました。");
    }

    private static void ClampOne(GameObject flask)
    {
        var anchor = FindDeepChild(flask.transform, ANCHOR_NAME);
        if (anchor == null)
        {
            Debug.LogWarning($"{flask.name}: {ANCHOR_NAME} が見つかりません。");
            return;
        }

        // ElementVisual を探す（Anchor配下ならどこでもOK）
        var vis = FindFirstContains(anchor, "ElementVisual");
        if (vis == null)
        {
            Debug.LogWarning($"{flask.name}: ElementVisual が見つかりません（先に追加してから実行してください）");
            return;
        }

        // 物理暴れ防止（VFX側のCollider/RBは全部無効）
        DisablePhysicsRecursively(vis.gameObject);

        // 親は VFXVolumeMesh があればそこ、無ければ Anchor
        var volume = FindDeepChild(anchor, VOLUME_NAME);
        var parent = volume != null ? volume : anchor;

        Undo.SetTransformParent(vis, parent, "Parent ElementVisual");
        Undo.RecordObject(vis, "Reset Transform");
        vis.localPosition = new Vector3(0f, Y_OFFSET, 0f);
        vis.localRotation = Quaternion.identity;

        // ★強制縮小（安全サイズ）
        vis.localScale = Vector3.one * SAFE_INNER_SCALE;

        // さらに確実に：子のSolid/Liquid/Gasの MeshCollider が残ってると嫌なのでOFF
        DisableChildMeshColliders(vis.gameObject);
    }

    private static void DisablePhysicsRecursively(GameObject go)
    {
        var rbs = go.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs)
        {
            Undo.RecordObject(rb, "Disable Rigidbody");
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        var cols = go.GetComponentsInChildren<Collider>(true);
        foreach (var col in cols)
        {
            Undo.RecordObject(col, "Disable Collider");
            col.enabled = false;
        }
    }

    private static void DisableChildMeshColliders(GameObject go)
    {
        var mcs = go.GetComponentsInChildren<MeshCollider>(true);
        foreach (var mc in mcs)
        {
            Undo.RecordObject(mc, "Disable MeshCollider");
            mc.enabled = false;
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindFirstContains(Transform parent, string contains)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name.Contains(contains)) return child;
        }
        return null;
    }
}
