using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public string selectedElementName = "";
    public string selectedEquipmentName = "";

    public ChemElementDatabase db;
    public ReactionPredictor predictor;
    public ChemReactionAnimator animator;

    public Transform spawnParent;
    public GameObject sourceVessel;

    private GameObject currentInstance;
    private ParticleSystem liquidParticle;
    private Renderer liquidSurface;

    private Vector3 lastPos;
    private Quaternion lastRot;
    private string lastElement = "";

    // ============================================================
    // ELEMENT 選択
    // ============================================================
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;
        lastElement = symbol;

        SpawnFlask();
        ApplyAppearance(db.GetColor(symbol), symbol);
    }

    // EQUIPMENT（形式上必要）
    public void SelectEquipment(string equip)
    {
        selectedEquipmentName = equip;
    }

    // ============================================================
    // FLASK を生成（Scale=42 + 構造復元 + 描画順固定）
    // ============================================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);
        if (currentInstance == null)
        {
            Debug.LogError("sourceVessel は必ず Prefab を設定してください");
            return;
        }

        currentInstance.transform.SetParent(spawnParent, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = new Vector3(42f, 42f, 42f);

        Transform container = currentInstance.transform.Find("LiquidContainer");
        if (container == null)
        {
            Debug.LogError("LiquidContainer が Prefab 内にありません");
            return;
        }

        Transform lp = container.Find("LiquidParticle");
        if (lp != null) liquidParticle = lp.GetComponent<ParticleSystem>();

        Transform ls = container.Find("LiquidSurface");
        if (ls != null) liquidSurface = ls.GetComponent<Renderer>();

        ConfigureLiquidParticle();
        FixRenderingOrder();

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // ============================================================
    // Particle 設定（Mesh 内に閉じ込める & 初期非表示）
    // ============================================================
    private void ConfigureLiquidParticle()
    {
        if (liquidParticle == null) return;

        var emission = liquidParticle.emission;
        emission.enabled = false;

        var main = liquidParticle.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSize = 0.02f;

        var shape = liquidParticle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;

        MeshFilter mf = currentInstance.transform.Find("Model").GetComponent<MeshFilter>();
        if (mf != null) shape.mesh = mf.sharedMesh;

        shape.normalOffset = -0.02f;

        ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
        pr.material.renderQueue = 2700;
        pr.material.SetInt("_ZWrite", 0);
    }

    // ============================================================
    // 描画順序：Wireframe → Surface → Particle
    // ============================================================
    private void FixRenderingOrder()
    {
        MeshRenderer mr = currentInstance.transform.Find("Model").GetComponent<MeshRenderer>();
        Material wireMat = mr.material;
        wireMat.renderQueue = 2500;
        wireMat.SetInt("_ZWrite", 0);

        if (liquidSurface != null)
        {
            Material surfMat = liquidSurface.material;
            surfMat.renderQueue = 2600;
            surfMat.SetInt("_ZWrite", 0);
        }

        if (liquidParticle != null)
        {
            ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
            Material liqMat = pr.material;
            liqMat.renderQueue = 2700;
            liqMat.SetInt("_ZWrite", 0);
        }
    }

    // ============================================================
    // 色の適用
    // ============================================================
    private void ApplyAppearance(Color col, string symbol)
    {
        if (liquidParticle != null)
        {
            var main = liquidParticle.main;
            main.startColor = col;
        }

        if (liquidSurface != null)
            liquidSurface.material.SetColor("_Color", col);
    }

    // ============================================================
    // 振ったときの揺れ
    // ============================================================
    private void Update()
    {
        if (currentInstance == null || liquidParticle == null) return;

        Vector3 pos = currentInstance.transform.position;
        Quaternion rot = currentInstance.transform.rotation;

        float move = (pos - lastPos).magnitude * 20f;
        float spin = Quaternion.Angle(rot, lastRot) * 0.2f;
        float shake = Mathf.Clamp01(move + spin);

        var noise = liquidParticle.noise;
        noise.enabled = true;
        noise.strength = 0.02f + shake * 0.25f;

        lastPos = pos;
        lastRot = rot;
    }

    // ============================================================
    // 化学反応
    // ============================================================
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

    public void _ResetExperiment()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        lastElement = "";
    }
}
