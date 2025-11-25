using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    // --- 外部参照 ---
    public ChemElementDatabase db;
    public ReactionPredictor predictor;
    public ChemReactionAnimator animator;

    // --- プレハブ設定 ---
    public Transform spawnParent;
    public GameObject sourceVessel;

    // --- 液体水面（あっても無くても可） ---
    public MeshRenderer liquidSurface;

    // --- 環境値 ---
    public float temperature = 25f;
    public float humidity = 50f;
    public float pressure = 1f;

    // --- 内部 ---
    private GameObject currentInstance;
    private ParticleSystem insideParticle;

    private Vector3 lastPos;
    private Quaternion lastRot;

    private string lastElement = "";

    // --- 旧API互換 ---
    public string selectedElementName = "";
    public string selectedEquipmentName = "";

    // =========================================================
    // 元素選択（UIや外部から呼び出されるメインAPI）
    // =========================================================
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;
        lastElement = symbol;

        SpawnFlask();                // ← フラスコ生成
        ApplyAppearanceFromDB(symbol); // ← 元素ごとの液体見た目
    }

    public void SelectEquipment(string name)
    {
        selectedEquipmentName = name; // 旧システム互換
    }

    // =========================================================
    // フラスコ生成
    // =========================================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);
        currentInstance.transform.SetParent(spawnParent, true);

        // 中のパーティクル取得
        insideParticle = currentInstance.transform.Find("Particle").GetComponent<ParticleSystem>();

        FixRenderingOrder();
        ConfigureParticleAsLiquid();     // ← 液体ボリューム初期化

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // =========================================================
    // レンダリング整列（ワイヤーフレーム → 液体）
    // =========================================================
    private void FixRenderingOrder()
    {
        // Wireframe モデル
        MeshRenderer wire = currentInstance.transform.Find("Model").GetComponent<MeshRenderer>();
        Material wireMat = wire.material;
        wireMat.renderQueue = 3100;
        wireMat.SetInt("_ZWrite", 0);

        // 内部液体パーティクル
        ParticleSystemRenderer pr = insideParticle.GetComponent<ParticleSystemRenderer>();
        Material liquidMat = pr.material;
        liquidMat.renderQueue = 3000;
        liquidMat.SetInt("_ZWrite", 0);

        pr.sortingOrder = 10;

        // Bounds拡大 → 遠距離でも見える
        foreach (var r in currentInstance.GetComponentsInChildren<MeshRenderer>())
            r.localBounds = new Bounds(Vector3.zero, Vector3.one * 5000f);
    }

    // =========================================================
    // パーティクルを“液体ボリューム”に初期化
    // =========================================================
    private void ConfigureParticleAsLiquid()
    {
        if (insideParticle == null) return;

        var main = insideParticle.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.startSpeed = 0.00f;    // ← 外に飛ばないよう ZERO にする
        main.startSize = 0.025f;
        main.startLifetime = 6f;

        // フラスコ内部に閉じ込めるための形状指定（最重要）
        var shape = insideParticle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;

        // ← 実際のフラスコモデルに完全一致させる
        shape.scale = new Vector3(0.38f, 0.78f, 0.38f);

        // Noise は流体感のために弱める（今は強すぎて外に飛ぶ）
        var noise = insideParticle.noise;
        noise.enabled = true;
        noise.strength = 0.05f;        // ← ここを弱くする
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.1f;
        noise.positionAmount = 0f;

        // 外に飛ばす原因になる collision は OFF
        var collision = insideParticle.collision;
        collision.enabled = false;

        var renderer = insideParticle.GetComponent<ParticleSystemRenderer>();
        renderer.material.renderQueue = 3000;
        renderer.material.SetInt("_ZWrite", 0);
    }

    // =========================================================
    // 元素＋環境によって液体の見た目を変える（最終正解）
    // =========================================================
    private void ApplyAppearanceFromDB(string symbol)
    {
        if (insideParticle == null) return;

        Color baseCol = (Color)db.GetColor(symbol);

        float tempFactor = Mathf.Clamp(temperature / 50f, 0.5f, 1.4f);
        float alpha = Mathf.Clamp(humidity / 100f, 0.2f, 1f);

        Color final = baseCol * tempFactor;
        final.a = alpha;

        var main = insideParticle.main;
        main.startColor = final;

        // 粘度は強く適用（外に飛ばないよう抑制）
        float visc = db.GetViscosity(symbol);
        float density = db.GetDensity(symbol);

        // 粘度で速度を調整（外に出ない）
        main.startSpeed = Mathf.Clamp(0.015f / visc, 0.001f, 0.02f);

        // ノイズ強度は密度に比例するが、外にあふれないよう制限する
        var noise = insideParticle.noise;
        noise.strength = Mathf.Clamp(density * 0.05f, 0.01f, 0.10f);
    }

    // =========================================================
    // VR 手振りで液体揺れ
    // =========================================================
    private void Update()
    {
        if (currentInstance == null || insideParticle == null) return;

        Vector3 pos = currentInstance.transform.position;
        float move = (pos - lastPos).magnitude * 25f;

        Quaternion rot = currentInstance.transform.rotation;
        float rotSpeed = Quaternion.Angle(rot, lastRot) * 0.15f;

        float shake = Mathf.Clamp(move + rotSpeed, 0f, 2f);

        var noise = insideParticle.noise;

        float baseStrength = noise.strength.constant;

        float newStrength = Mathf.Clamp(
            baseStrength + (shake * 0.03f),
            0.01f,
            0.12f // ← 上限を設けて外に漏れないようにする
        );

        noise.strength = new ParticleSystem.MinMaxCurve(newStrength);

        noise.frequency = 0.5f + shake * 0.8f;

        lastPos = pos;
        lastRot = rot;
    }

    // =========================================================
    // 化学反応（外部UIが呼ぶ）
    // =========================================================
    public void CombineWith(string next)
    {
        string reaction = predictor.Predict(lastElement, next);

        if (reaction.Contains("塩")) animator.PlayFoam(insideParticle);
        if (reaction.Contains("酸化")) animator.PlayHeat(insideParticle);
        if (reaction.Contains("金属")) animator.PlaySpark(insideParticle);
        if (reaction.Contains("発光")) animator.PlayGlow(currentInstance);
        if (reaction.Contains("波")) animator.PlayWave(insideParticle);

        lastElement = next;
    }

    // =========================================================
    public void ResetExperiment()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        lastElement = "";
    }
}
