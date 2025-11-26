using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("=== References ===")]
    public ChemElementDatabase db;
    public ReactionPredictor predictor;
    public ChemReactionAnimator animator;

    public Transform spawnParent;
    public GameObject sourceVessel;

    [Header("Environment")]
    public float temperature = 25f;   // °C
    public float humidity = 50f;      // %
    public float pressure = 1f;       // atm

    private GameObject currentInstance;
    private ParticleSystem insideParticle;

    private string lastElement = "";

    private Vector3 lastPos;
    private Quaternion lastRot;

    // 旧 API 互換（VRC UI が参照しているため残す）
    public string selectedElementName = "";
    public string selectedEquipmentName = "";

    // ---------------------------------------------------------
    // 元素を選択したときに呼ばれる
    // ---------------------------------------------------------
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;

        SpawnFlask();

        lastElement = symbol;

        Color32 baseCol = db.GetColor(symbol);
        ApplyAppearanceFromDatabase(baseCol, symbol);
    }

    public void SelectEquipment(string name)
    {
        selectedEquipmentName = name;
    }

    // ---------------------------------------------------------
    // フラスコ生成
    // ---------------------------------------------------------
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);
        currentInstance.transform.SetParent(spawnParent, true);

        insideParticle = currentInstance.transform.Find("Particle").GetComponent<ParticleSystem>();

        ConfigureParticleAsLiquid();
        FixRenderingOrder();

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // ---------------------------------------------------------
    // パーティクルを “フラスコの中に閉じ込める”
    // ---------------------------------------------------------
    private void ConfigureParticleAsLiquid()
    {
        if (insideParticle == null) return;

        var main = insideParticle.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.startSpeed = 0f;
        main.startSize = 0.025f;
        main.startLifetime = 5f;

        var shape = insideParticle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;

        // ※ 必要ならフラスコの大きさに合わせて調整する
        shape.scale = new Vector3(0.38f, 0.78f, 0.38f);

        var noise = insideParticle.noise;
        noise.enabled = true;
        noise.strength = 0.05f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.1f;

        var renderer = insideParticle.GetComponent<ParticleSystemRenderer>();
        renderer.material.renderQueue = 3000;
        renderer.material.SetInt("_ZWrite", 0);
    }

    // ---------------------------------------------------------
    // 描画優先順位（Wireframe → Liquid → その他）
    // ---------------------------------------------------------
    private void FixRenderingOrder()
    {
        MeshRenderer wire = currentInstance.transform.Find("Model").GetComponent<MeshRenderer>();
        Material wireMat = wire.material;
        wireMat.renderQueue = 3100;
        wireMat.SetInt("_ZWrite", 0);

        ParticleSystemRenderer pr = insideParticle.GetComponent<ParticleSystemRenderer>();
        Material liquid = pr.material;
        liquid.renderQueue = 3000;
        liquid.SetInt("_ZWrite", 0);

        pr.sortingOrder = 10;

        MeshRenderer[] mrs = currentInstance.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in mrs)
        {
            r.localBounds = new Bounds(Vector3.zero, Vector3.one * 9999f);
        }
    }

    // ---------------------------------------------------------
    // 元素ごとの動的見た目
    // ---------------------------------------------------------
    private void ApplyAppearanceFromDatabase(Color32 baseCol32, string symbol)
    {
        if (insideParticle == null) return;

        Color baseColor = (Color)baseCol32;

        float tempFactor = Mathf.Clamp(temperature / 50f, 0.5f, 1.4f);
        float alpha = Mathf.Clamp(humidity / 100f, 0.3f, 1f);

        Color finalColor = baseColor * tempFactor;
        finalColor.a = alpha;

        var main = insideParticle.main;
        main.startColor = finalColor;

        float viscosity = db.GetViscosity(symbol);
        float density = db.GetDensity(symbol);

        // 粘度 → 流れにくさ
        main.startSpeed = Mathf.Clamp(0.015f / viscosity, 0.001f, 0.02f);

        // 密度 → ノイズ強さ
        var noise = insideParticle.noise;
        noise.strength = Mathf.Clamp(density * 0.05f, 0.01f, 0.10f);
    }

    // ---------------------------------------------------------
    // VR 振った時の流体揺れ
    // ---------------------------------------------------------
    private void Update()
    {
        if (currentInstance == null || insideParticle == null) return;

        Vector3 pos = currentInstance.transform.position;
        Quaternion rot = currentInstance.transform.rotation;

        float move = (pos - lastPos).magnitude * 25f;
        float rotSpeed = Quaternion.Angle(rot, lastRot) * 0.15f;

        float shake = Mathf.Clamp(move + rotSpeed, 0f, 2f);

        var noise = insideParticle.noise;
        float baseStrength = noise.strength.constant;

        float newStrength = Mathf.Clamp(baseStrength + (shake * 0.03f), 0.01f, 0.12f);

        noise.strength = new ParticleSystem.MinMaxCurve(newStrength);
        noise.frequency = 0.5f + shake * 0.8f;

        lastPos = pos;
        lastRot = rot;
    }

    // ---------------------------------------------------------
    // 化学反応（泡・発熱・火花・発光・波紋）
    // ---------------------------------------------------------
    public void CombineWith(string next)
    {
        string reaction = predictor.Predict(lastElement, next);

        Debug.Log("[ChemLab Reaction] " + reaction);

        if (reaction.Contains("塩"))
            animator.PlayFoam(insideParticle);

        if (reaction.Contains("酸化"))
            animator.PlayHeat(insideParticle);

        if (reaction.Contains("金属"))
            animator.PlaySpark(insideParticle);

        if (reaction.Contains("発光"))
            animator.PlayGlow(currentInstance);

        if (reaction.Contains("波"))
            animator.PlayWave(insideParticle);

        lastElement = next;
    }

    // ---------------------------------------------------------
    // 実験リセット
    // ---------------------------------------------------------
    public void ResetExperiment()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        lastElement = "";
    }
}
