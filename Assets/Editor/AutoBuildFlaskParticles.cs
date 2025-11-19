#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AutoBuildFlaskParticles : EditorWindow
{
    [MenuItem("VRC ChemLab/Build Flask From Transform")]
    public static void BuildFlask()
    {
        // ================================
        // 1. Scene の CONICAL_FLASK を取得
        // ================================
        GameObject flask = GameObject.Find("CONICAL_FLASK");
        if (flask == null)
        {
            Debug.LogError("[BuildFlask] Scene に CONICAL_FLASK がありません。");
            return;
        }
        Debug.Log("[BuildFlask] CONICAL_FLASK を検出");

        // Flask の Transform（ワールド座標）
        Vector3 F_pos = flask.transform.position;
        Vector3 F_scale = flask.transform.lossyScale;
        Quaternion F_rot = flask.transform.rotation;

        // ================================
        // 2. 不要オブジェクトの削除
        // ================================
        string[] removeList = {
            "Body",
            "MeshRenderer",
            "MeshFilter",
            "Particle System",
            "OverflowParticle",
            "LiquidParticles"
        };

        foreach (string name in removeList)
        {
            Transform t = flask.transform.Find(name);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
                Debug.Log("[BuildFlask] 削除：" + name);
            }
        }

        // ================================
        // 3. WireframeShell の Transform を取得
        // ================================
        Transform wire = flask.transform.Find("WireframeShell");
        if (wire == null)
        {
            Debug.LogError("[BuildFlask] WireframeShell が見つかりません。");
            return;
        }

        Vector3 W_localPos = wire.localPosition;
        Vector3 W_localScale = wire.localScale;
        Quaternion W_localRot = wire.localRotation;

        // ================================
        // 4. LiquidParticles（内部液体）作成
        // ================================
        GameObject liquid = new GameObject("LiquidParticles");
        liquid.transform.SetParent(flask.transform);

        // フラスコの真ん中に配置（ワイヤーフレーム依存）
        liquid.transform.localPosition = W_localPos + new Vector3(0, W_localScale.y * 0.2f, 0);
        liquid.transform.localRotation = W_localRot;
        liquid.transform.localScale = W_localScale * 0.45f;

        ParticleSystem ps = liquid.AddComponent<ParticleSystem>();
        var main = ps.main;
        var em = ps.emission;
        var shape = ps.shape;

        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.15f;
        main.startSize = 0.045f;
        main.gravityModifier = 0.0f;

        em.rateOverTime = 160;

        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f * W_localScale.x; // フラスコスケールに合わせる

        // Material
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.color = new Color(1, 1, 1, 0.7f);

        var renderer = liquid.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = 10;

        Debug.Log("[BuildFlask] LiquidParticles を生成");

        // ================================
        // 5. OverflowParticle を WireframeShell の “上” に作成
        // ================================
        GameObject overflow = new GameObject("OverflowParticle");
        overflow.transform.SetParent(flask.transform);

        overflow.transform.localPosition =
            W_localPos + new Vector3(0, W_localScale.y * 0.95f, 0);

        overflow.transform.localRotation = W_localRot;

        ParticleSystem ov = overflow.AddComponent<ParticleSystem>();
        var ovMain = ov.main;
        var ovEm = ov.emission;

        ovMain.startLifetime = 0.3f;
        ovMain.startSpeed = 0.3f;
        ovMain.startSize = 0.04f;

        ovEm.rateOverTime = 0;

        var ovR = overflow.GetComponent<ParticleSystemRenderer>();
        ovR.material = mat;

        Debug.Log("[BuildFlask] OverflowParticle を生成");

        // ================================
        // 6. ChemElementSpawner にすべて自動アサイン
        // ================================
        ChemElementSpawner spawner = GameObject.FindObjectOfType<ChemElementSpawner>();

        if (spawner != null)
        {
            spawner.liquidParticles = ps;
            spawner.overflowParticlePrefab = overflow;
            spawner.sourceVessel = flask;
            spawner.spawnParent = flask.transform.parent;

            Debug.Log("[BuildFlask] ChemElementSpawner に自動設定完了！");
        }
        else
        {
            Debug.LogWarning("[BuildFlask] Scene に ChemElementSpawner がありません");
        }

        Debug.Log("[BuildFlask] === 完了：CONICAL_FLASK の位置を読み込み構築 ===");
    }
}
#endif