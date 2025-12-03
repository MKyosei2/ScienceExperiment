using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public enum MatterState
{
    Solid,
    Liquid,
    Gas
}

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemElementDatabase db;                    // 元素データ
    public ChemEnvironmentManager environment;        // 温度などの環境

    public Transform spawnParent;                     // 生成位置
    public GameObject sourceVessel;                   // フラスコPrefab

    private GameObject currentInstance;
    private ParticleSystem liquidParticle;
    private Renderer liquidSurface;
    private string lastElement = "";

    private Vector3 lastPos;
    private Quaternion lastRot;
    public string selectedElementName = "";
    public string selectedEquipmentName = "";

    // ============================================================
    // ELEMENT SELECT
    // ============================================================
    public void SelectElement(string symbol)
    {
        lastElement = symbol;

        SpawnFlask();
        ApplyMatterState(symbol);
    }

    // ============================================================
    // EQUIPMENT (UIと互換保持のため空実装)
    // ============================================================
    public void SelectEquipment(string equip)
    {
        // 必要なら機器選択処理を後で追加
    }

    // ============================================================
    // SPAWN FLASK
    // ============================================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);

        if (currentInstance == null)
        {
            Debug.LogError("sourceVessel が設定されていません");
            return;
        }

        currentInstance.transform.SetParent(spawnParent, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = new Vector3(42f, 42f, 42f);

        //------------------------------------------
        // ModelRoot/Model の取得
        //------------------------------------------
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

        MeshRenderer modelRenderer = model.GetComponent<MeshRenderer>();
        MeshFilter modelMesh = model.GetComponent<MeshFilter>();

        //------------------------------------------
        // LiquidContainer の取得
        //------------------------------------------
        Transform container = currentInstance.transform.Find("LiquidContainer");
        if (container == null)
        {
            Debug.LogError("LiquidContainer がありません");
            return;
        }

        Transform ls = container.Find("LiquidSurface");
        Transform lp = container.Find("LiquidParticle");

        if (ls != null) liquidSurface = ls.GetComponent<Renderer>();
        if (lp != null) liquidParticle = lp.GetComponent<ParticleSystem>();

        ConfigureLiquidParticle(modelMesh);
        FixRenderingOrder(modelRenderer);

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // ============================================================
    // PARTICLE CONFIG
    // ============================================================
    private void ConfigureLiquidParticle(MeshFilter modelMesh)
    {
        if (liquidParticle == null || modelMesh == null) return;

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
        shape.mesh = modelMesh.sharedMesh;
        shape.normalOffset = -0.02f;

        ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
        pr.material.renderQueue = 2700;
        pr.material.SetInt("_ZWrite", 0);
    }

    // ============================================================
    // RENDER ORDER
    // ============================================================
    private void FixRenderingOrder(MeshRenderer modelRenderer)
    {
        Material wire = modelRenderer.material;
        wire.renderQueue = 2500;
        wire.SetInt("_ZWrite", 0);

        if (liquidSurface != null)
        {
            Material surf = liquidSurface.material;
            surf.renderQueue = 2600;
            surf.SetInt("_ZWrite", 0);
        }

        if (liquidParticle != null)
        {
            Material pm = liquidParticle.GetComponent<ParticleSystemRenderer>().material;
            pm.renderQueue = 2700;
            pm.SetInt("_ZWrite", 0);
        }
    }

    // ============================================================
    // MATTER STATE LOGIC
    // ============================================================
    private MatterState GetState(string symbol)
    {
        float T = environment.Temperature;

        float melt = db.GetMeltingPoint(symbol);
        float boil = db.GetBoilingPoint(symbol);

        if (T < melt) return MatterState.Solid;
        if (T < boil) return MatterState.Liquid;
        return MatterState.Gas;
    }

    //----------------------------------------------------------
    // Solid
    //----------------------------------------------------------
    private void ApplySolidAppearance(string symbol)
    {
        Color col = db.GetColor(symbol);

        if (liquidSurface != null)
            liquidSurface.material.color = new Color(col.r * 0.6f, col.g * 0.6f, col.b * 0.6f, 1f);

        if (liquidParticle != null)
            liquidParticle.Stop();
    }

    //----------------------------------------------------------
    // Liquid
    //----------------------------------------------------------
    private void ApplyLiquidAppearance(string symbol)
    {
        Color col = db.GetColor(symbol);

        if (liquidSurface != null)
            liquidSurface.material.color = col;

        if (liquidParticle != null)
        {
            var em = liquidParticle.emission;
            em.enabled = false;
        }
    }

    //----------------------------------------------------------
    // Gas
    //----------------------------------------------------------
    private void ApplyGasAppearance(string symbol)
    {
        Color col = db.GetColor(symbol);

        if (liquidSurface != null)
            liquidSurface.material.color = new Color(0, 0, 0, 0);

        if (liquidParticle != null)
        {
            var em = liquidParticle.emission;
            em.enabled = true;

            var main = liquidParticle.main;
            main.startColor = new Color(col.r, col.g, col.b, 0.25f);
        }
    }

    // ============================================================
    // APPLY MATTER STATE
    // ============================================================
    private void ApplyMatterState(string symbol)
    {
        MatterState s = GetState(symbol);

        if (s == MatterState.Solid)
            ApplySolidAppearance(symbol);
        else if (s == MatterState.Liquid)
            ApplyLiquidAppearance(symbol);
        else
            ApplyGasAppearance(symbol);
    }

    // ============================================================
    // SHAKE NOISE + STATE UPDATE
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

        // 温度変化に応じた状態更新
        if (lastElement != "")
            ApplyMatterState(lastElement);
    }
}
