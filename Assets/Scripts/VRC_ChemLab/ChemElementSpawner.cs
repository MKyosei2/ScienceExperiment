using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ChemElementSpawner : UdonSharpBehaviour
{
    // ================================
    // 公開フィールド（元からあったもの）
    // ================================
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

    // ================================
    // UI 連携のために追加
    // ================================
    private string elementHistory = "";           // 元素履歴
    public string lastEquipmentName = "";         // 最後に選んだ器具名

    public void AddElementToHistory(string symbol)
    {
        if (!string.IsNullOrEmpty(symbol))
            elementHistory += symbol + ", ";
    }

    public string GetElementHistory()
    {
        return elementHistory;
    }

    // ================================
    // 元素選択
    // ================================
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;
        lastElement = symbol;

        AddElementToHistory(symbol);  // ★ UI用の履歴に追加

        SpawnFlask();
        ApplyAppearance(db.GetColor(symbol), symbol);
    }

    // ================================
    // 器具選択（UIに反映）
    // ================================
    public void SelectEquipment(string equip)
    {
        selectedEquipmentName = equip;
        lastEquipmentName = equip;   // ★ UI用の器具名を保存
    }

    // ================================
    // フラスコ生成処理
    // ================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);

        if (currentInstance == null)
        {
            Debug.LogError("sourceVessel は Prefab を設定してください");
            return;
        }

        currentInstance.transform.SetParent(spawnParent, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = new Vector3(42, 42, 42);

        // ModelRoot → Model の取得
        Transform modelRoot = currentInstance.transform.Find("ModelRoot");
        if (modelRoot == null)
        {
            Debug.LogError("ModelRoot が見つかりません");
            return;
        }

        Transform model = modelRoot.Find("Model");
        if (model == null)
        {
            Debug.LogError("ModelRoot 内に Model がありません");
            return;
        }

        MeshFilter mf = model.GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("Model に MeshFilter がありません");
            return;
        }

        // LiquidContainer
        Transform container = currentInstance.transform.Find("LiquidContainer");
        if (container == null)
        {
            Debug.LogError("LiquidContainer が見つかりません");
            return;
        }

        Transform lp = container.Find("LiquidParticle");
        if (lp != null)
            liquidParticle = lp.GetComponent<ParticleSystem>();

        Transform ls = container.Find("LiquidSurface");
        if (ls != null)
            liquidSurface = ls.GetComponent<Renderer>();

        ConfigureLiquidParticle();
        FixRenderingOrder();

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // ================================
    // Particle設定
    // ================================
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

        Transform model = currentInstance.transform.Find("ModelRoot/Model");
        if (model != null)
        {
            MeshFilter mf = model.GetComponent<MeshFilter>();
            if (mf != null)
                shape.mesh = mf.sharedMesh;
        }

        shape.normalOffset = -0.02f;

        ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
        pr.material.renderQueue = 2700;
        pr.material.SetInt("_ZWrite", 0);
    }

    // ================================
    // 描画順序の修正
    // ================================
    private void FixRenderingOrder()
    {
        Transform model = currentInstance.transform.Find("ModelRoot/Model");
        if (model != null)
        {
            MeshRenderer mr = model.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material.renderQueue = 2500;
                mr.material.SetInt("_ZWrite", 0);
            }
        }

        if (liquidSurface != null)
        {
            liquidSurface.material.renderQueue = 2600;
            liquidSurface.material.SetInt("_ZWrite", 0);
        }

        if (liquidParticle != null)
        {
            ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
            pr.material.renderQueue = 2700;
            pr.material.SetInt("_ZWrite", 0);
        }
    }

    // ================================
    // 色の適用
    // ================================
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

    // ================================
    // 振った時の揺れ
    // ================================
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

    // ================================
    // 化学反応
    // ================================
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

    // ================================
    // リセット
    // ================================
    public void _ResetExperiment()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        lastElement = "";
    }
}
