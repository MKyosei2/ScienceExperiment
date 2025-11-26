using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("=== Database & Systems ===")]
    public ChemElementDatabase db;
    public ReactionPredictor predictor;
    public ChemReactionAnimator animator;

    [Header("=== Spawn Settings ===")]
    public Transform spawnParent;
    public GameObject sourceVessel;        // CONICAL_FLASK prefab（唯一の液体容器）

    [Header("=== Environment ===")]
    public float temperature = 25f;        // 0〜100°C
    public float humidity = 50f;           // 0〜100%
    public float pressure = 1f;            // 0.5〜2atm

    // --- Internal State ---
    private GameObject currentInstance;
    private ParticleSystem liquidParticle;
    private Renderer liquidSurface;

    private Vector3 lastPos;
    private Quaternion lastRot;
    private string lastElement = "";

    // compatibility with SpawnSelectorButton
    public string selectedElementName = "";
    public string selectedEquipmentName = "";

    // --------------------------------------------------------------
    // 元素選択
    // --------------------------------------------------------------
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;
        lastElement = symbol;

        SpawnFlask();

        Color32 baseCol = db.GetColor(symbol);
        ApplyAppearance((Color)baseCol, symbol);
    }

    // --------------------------------------------------------------
    // **旧API互換用（呼ばれるだけ / 空処理でOK）**
    // --------------------------------------------------------------
    public void SelectEquipment(string name)
    {
        selectedEquipmentName = name;
        // 現状は何もしない（後で器具切替機能を追加可能）
    }

    // --------------------------------------------------------------
    // フラスコ生成
    // --------------------------------------------------------------
    private void SpawnFlask()
    {
        // 既存破棄
        if (currentInstance != null)
            Destroy(currentInstance);

        // 生成
        currentInstance = VRCInstantiate(sourceVessel);
        currentInstance.transform.SetParent(spawnParent, false);

        // 正しい LiquidContainer → LiquidSurface / LiquidParticle を取得
        Transform container = currentInstance.transform.Find("LiquidContainer");
        if (container == null)
        {
            Debug.LogError("LiquidContainer が見つかりません");
            return;
        }

        liquidParticle = container.Find("LiquidParticle").GetComponent<ParticleSystem>();
        liquidSurface = container.Find("LiquidSurface").GetComponent<Renderer>();

        ConfigureLiquidParticle();
        FixRenderingOrder();

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // --------------------------------------------------------------
    // パーティクル（内部液体）の設定
    // --------------------------------------------------------------
    private void ConfigureLiquidParticle()
    {
        if (liquidParticle == null) return;

        var main = liquidParticle.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.startSpeed = 0f;        // 内部液体は落下しない
        main.startLifetime = 4f;
        main.startSize = 0.025f;

        var shape = liquidParticle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.38f, 0.78f, 0.38f);  // Flask 内部サイズに合わせて調整
        shape.position = new Vector3(0f, 0.10f, 0f);

        var noise = liquidParticle.noise;
        noise.enabled = true;
        noise.strength = 0.05f;
        noise.frequency = 0.5f;

        var renderer = liquidParticle.GetComponent<ParticleSystemRenderer>();
        renderer.material.renderQueue = 3000;
        renderer.material.SetInt("_ZWrite", 0);
    }

    // --------------------------------------------------------------
    // 描画順（ワイヤーフレーム → 液体 → 表面）
    // --------------------------------------------------------------
    private void FixRenderingOrder()
    {
        // Flask Model
        MeshRenderer wire = currentInstance.transform.Find("Model").GetComponent<MeshRenderer>();
        Material wireMat = wire.material;
        wireMat.renderQueue = 3100;       // 前面
        wireMat.SetInt("_ZWrite", 0);

        // 内部液体（Particle）
        ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
        Material liquid = pr.material;
        liquid.renderQueue = 3000;
        liquid.SetInt("_ZWrite", 0);

        // 液体表面（Shader）
        if (liquidSurface != null)
        {
            liquidSurface.material.renderQueue = 2995;
            liquidSurface.material.SetInt("_ZWrite", 0);
        }
    }

    // --------------------------------------------------------------
    // 元素に応じた見た目（色 / 粘度 / 密度）
    // --------------------------------------------------------------
    private void ApplyAppearance(Color baseColor, string symbol)
    {
        if (liquidParticle == null || liquidSurface == null) return;

        float tempFactor = Mathf.Clamp(temperature / 50f, 0.5f, 1.4f);
        float alpha = Mathf.Clamp(humidity / 100f, 0.3f, 1f);

        Color col = baseColor * tempFactor;
        col.a = alpha;

        // 内部液体色
        var main = liquidParticle.main;
        main.startColor = col;

        // 表面色
        liquidSurface.material.SetColor("_Color", col);

        // 粘度・密度の反映
        float visc = db.GetViscosity(symbol);
        float density = db.GetDensity(symbol);

        main.startSpeed = Mathf.Clamp(0.02f / visc, 0.002f, 0.04f);

        var noise = liquidParticle.noise;
        noise.strength = Mathf.Clamp(density * 0.05f, 0.02f, 0.15f);
    }

    // --------------------------------------------------------------
    // VR：振った時の液体揺れ
    // --------------------------------------------------------------
    private void Update()
    {
        if (currentInstance == null || liquidParticle == null) return;

        Vector3 pos = currentInstance.transform.position;
        Quaternion rot = currentInstance.transform.rotation;

        float move = (pos - lastPos).magnitude * 25f;
        float rotSpeed = Quaternion.Angle(rot, lastRot) * 0.18f;

        float shake = Mathf.Clamp(move + rotSpeed, 0f, 2.2f);

        var noise = liquidParticle.noise;
        float baseStrength = noise.strength.constant;
        float newStrength = Mathf.Clamp(baseStrength + shake * 0.04f, 0.02f, 0.15f);

        noise.strength = new ParticleSystem.MinMaxCurve(newStrength);
        noise.frequency = 0.7f + shake * 0.8f;

        lastPos = pos;
        lastRot = rot;
    }

    // --------------------------------------------------------------
    // 化学反応（AI推論 → 視覚効果）
    // --------------------------------------------------------------
    public void CombineWith(string next)
    {
        string reaction = predictor.Predict(lastElement, next);

        if (reaction.Contains("塩")) animator.PlayFoam(liquidParticle);
        if (reaction.Contains("酸化")) animator.PlayHeat(liquidParticle);
        if (reaction.Contains("金属")) animator.PlaySpark(liquidParticle);
        if (reaction.Contains("発光")) animator.PlayGlow(currentInstance);
        if (reaction.Contains("波")) animator.PlayWave(liquidParticle);

        lastElement = next;
    }

    // --------------------------------------------------------------
    // リセット
    // --------------------------------------------------------------
    public void ResetExperiment()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        lastElement = "";
    }
}
