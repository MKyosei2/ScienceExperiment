using UnityEngine;
using UnityEditor;

public class ChemLab_OneShotSetup0128Test
{
    [MenuItem("Tools/ChemLab/One-shot Setup 0128Test (H + Cl + Empty Beaker Experiment)")]
    public static void Setup()
    {
        // =========================
        // 1) Root: 0128Test
        // =========================
        GameObject root = GameObject.Find("0128Test");
        if (root == null)
        {
            root = new GameObject("0128Test");
            Undo.RegisterCreatedObjectUndo(root, "Create 0128Test");
        }

        // =========================
        // 2) Children: Beaker / Flasks
        // =========================
        GameObject beaker = FindOrCreateChild(root.transform, "BEAKER_EMPTY");
        GameObject flaskH = FindOrCreateChild(root.transform, "CONICAL_FLASK_H");
        GameObject flaskCl = FindOrCreateChild(root.transform, "CONICAL_FLASK_Cl");

        // 配置（見やすい位置）
        Vector3 basePos = root.transform.position;
        beaker.transform.position = basePos + new Vector3(0f, 0.9f, 0f);
        flaskH.transform.position = basePos + new Vector3(-0.35f, 0.9f, 0.15f);
        flaskCl.transform.position = basePos + new Vector3(0.35f, 0.9f, 0.15f);

        // =========================
        // 3) Beaker: PourTarget
        // =========================
        Transform pourTarget = FindChildByExactName(beaker.transform, "PourTarget");
        if (pourTarget == null)
        {
            GameObject pt = new GameObject("PourTarget");
            Undo.RegisterCreatedObjectUndo(pt, "Create PourTarget");
            pt.transform.SetParent(beaker.transform, false);
            pt.transform.localPosition = new Vector3(0f, 0.18f, 0.02f); // 口付近の目安
            pt.transform.localRotation = Quaternion.identity;
            pourTarget = pt.transform;
        }

        // =========================
        // 4) Flask: Spout (注ぎ口)
        // =========================
        Transform spoutH = FindOrCreateSpout(flaskH.transform, "Spout_H");
        Transform spoutCl = FindOrCreateSpout(flaskCl.transform, "Spout_Cl");

        // =========================
        // 5) Reaction Manager: HClExplosionReaction on BEAKER_EMPTY
        // =========================
        HClExplosionReaction reaction = beaker.GetComponent<HClExplosionReaction>();
        if (reaction == null)
        {
            reaction = Undo.AddComponent<HClExplosionReaction>(beaker);
        }

        // 必須参照をセット（Inspector自動設定）
        reaction.flaskH = flaskH;
        reaction.flaskCl = flaskCl;
        reaction.beakerPourTarget = pourTarget;
        reaction.spoutH = spoutH;
        reaction.spoutCl = spoutCl;

        // 判定パラメータ（“最低限動く”寄りの安定設定）
        reaction.pourStartAngleDeg = 28f;     // 少し傾ければ注ぎ判定
        reaction.pourRadius = 0.18f;          // 判定半径広め（失敗しづらい）
        reaction.requiredPourSeconds = 0.20f; // 短め（すぐ投入扱い）
        reaction.flashSeconds = 0.15f;
        reaction.debugLog = false;

        // =========================
        // 6) Beaker FX: Light / Particle / Audio (全部BEAKER_EMPTY傘下に詰める)
        // =========================
        Light explosionLight = EnsureChildLight(beaker.transform, "ExplosionLight", new Vector3(0f, 0.22f, 0f));
        ParticleSystem explosionPs = EnsureChildParticles(beaker.transform, "ExplosionParticles", new Vector3(0f, 0.20f, 0f));
        AudioSource explosionAudio = EnsureChildAudio(beaker.transform, "ExplosionAudio", new Vector3(0f, 0.20f, 0f));

        reaction.explosionLight = explosionLight;
        reaction.explosionParticles = explosionPs;
        reaction.explosionAudio = explosionAudio;

        // =========================
        // 7) (Optional) Collider / Rigidbody (掴みやすくする最低限)
        // =========================
        EnsureBasicPhysics(beaker);
        EnsureBasicPhysics(flaskH);
        EnsureBasicPhysics(flaskCl);

        // =========================
        // 8) Save
        // =========================
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(beaker);
        EditorUtility.SetDirty(flaskH);
        EditorUtility.SetDirty(flaskCl);

        Debug.Log("✅ 0128Test の実験セットアップ完了（H + Cl + Empty Beaker / 参照済み）");
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = FindChildByExactName(parent, name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static Transform FindChildByExactName(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c != null && c.name == name) return c;
        }
        return null;
    }

    private static Transform FindOrCreateSpout(Transform flask, string spoutName)
    {
        Transform sp = FindChildByExactName(flask, spoutName);
        if (sp == null)
        {
            GameObject g = new GameObject(spoutName);
            Undo.RegisterCreatedObjectUndo(g, "Create " + spoutName);
            g.transform.SetParent(flask, false);
            g.transform.localPosition = new Vector3(0f, 0.20f, 0.08f); // 口の目安
            g.transform.localRotation = Quaternion.identity;
            sp = g.transform;
        }
        return sp;
    }

    private static Light EnsureChildLight(Transform parent, string name, Vector3 localPos)
    {
        Transform t = FindChildByExactName(parent, name);
        GameObject go;

        if (t == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }
        else
        {
            go = t.gameObject;
        }

        Light li = go.GetComponent<Light>();
        if (li == null) li = Undo.AddComponent<Light>(go);

        li.type = LightType.Point;
        li.range = 2.5f;
        li.intensity = 4.0f;
        li.enabled = false;
        return li;
    }

    private static ParticleSystem EnsureChildParticles(Transform parent, string name, Vector3 localPos)
    {
        Transform t = FindChildByExactName(parent, name);
        GameObject go;

        if (t == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }
        else
        {
            go = t.gameObject;
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null) ps = Undo.AddComponent<ParticleSystem>(go);

        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = 0.35f;
        main.startSpeed = 1.4f;
        main.startSize = 0.12f;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 50)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private static AudioSource EnsureChildAudio(Transform parent, string name, Vector3 localPos)
    {
        Transform t = FindChildByExactName(parent, name);
        GameObject go;

        if (t == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }
        else
        {
            go = t.gameObject;
        }

        AudioSource a = go.GetComponent<AudioSource>();
        if (a == null) a = Undo.AddComponent<AudioSource>(go);

        a.playOnAwake = false;
        a.spatialBlend = 1.0f; // 3D
        a.volume = 1.0f;
        a.minDistance = 0.3f;
        a.maxDistance = 6.0f;
        return a;
    }

    private static void EnsureBasicPhysics(GameObject go)
    {
        if (go == null) return;

        Collider col = go.GetComponent<Collider>();
        if (col == null)
        {
            // とりあえずBoxCollider（最低限）
            BoxCollider bc = Undo.AddComponent<BoxCollider>(go);
            bc.size = new Vector3(0.18f, 0.25f, 0.18f);
            bc.center = new Vector3(0f, 0.12f, 0f);
        }

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(go);
            rb.mass = 1.0f;
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }
}
