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
<<<<<<< Updated upstream
    private string lastElement = "";
=======
    public string selectedElementName = "";
    public string selectedEquipmentName = "";
    public BoxCollider spawnArea;   // 生成領域
    public float minDistance = 0.25f;  // 最低距離（フラスコ同士の）
    private GameObject[] flaskList = new GameObject[100];
    private int flaskCount = 0;
>>>>>>> Stashed changes

    // ============================================================
    // ELEMENT 選択
    // ============================================================
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;
<<<<<<< Updated upstream
        lastElement = symbol;

        SpawnFlask();
        ApplyAppearance(db.GetColor(symbol), symbol);
    }

    // EQUIPMENT（形式上必要）
=======

        ElementData data = db.GetElement(symbol);
        if (data == null)
        {
            Debug.LogError("Element not found: " + symbol);
            return;
        }

        SpawnFlask();
        ApplyAppearance(data);
    }


    // ============================================================
    // EQUIPMENT (UIと互換保持のため空実装)
    // ============================================================
>>>>>>> Stashed changes
    public void SelectEquipment(string equip)
    {
        selectedEquipmentName = equip;
    }

    // ============================================================
    // FLASK を生成（Scale=42 + 構造復元 + 描画順固定）
    // ============================================================
    //--------------------------------------
    // Prefab instantiate
    //--------------------------------------
    private void SpawnFlask()
    {
        if (sourceVessel == null)
        {
            Debug.LogError("sourceVessel は Prefab を設定してください");
            return;
        }

<<<<<<< Updated upstream
        // 親・位置・回転・スケール適用
        currentInstance.transform.SetParent(spawnParent, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = new Vector3(42, 42, 42);

        // ================================
        // ModelRoot / Model の取得（重要）
        // ================================
        Transform modelRoot = currentInstance.transform.Find("ModelRoot");
=======
        // 新しいフラスコ生成
        GameObject flask = VRCInstantiate(sourceVessel);
        currentInstance = flask;

        flask.transform.localScale = new Vector3(42, 42, 42);

        // ランダム配置
        Vector3 finalPos = GetValidRandomPosition();
        flask.transform.position = finalPos;

        // 登録
        flaskList[flaskCount] = flask;
        flaskCount++;

        // -------------------------
        // ModelRoot / Model
        // -------------------------
        Transform modelRoot = flask.transform.Find("ModelRoot");
>>>>>>> Stashed changes
        if (modelRoot == null)
        {
            Debug.LogError("ModelRoot が見つかりません");
            return;
        }

        Transform model = modelRoot.Find("Model");
        if (model == null)
        {
<<<<<<< Updated upstream
            Debug.LogError("ModelRoot の中に Model がありません");
=======
            Debug.LogError("Model が見つかりません");
>>>>>>> Stashed changes
            return;
        }

        MeshFilter mf = model.GetComponent<MeshFilter>();
        MeshRenderer mr = model.GetComponent<MeshRenderer>();

        if (mf == null || mr == null)
        {
<<<<<<< Updated upstream
            Debug.LogError("Model に MeshFilter / MeshRenderer がありません");
            return;
        }

        // ================================
        // LiquidContainer 系の取得
        // ================================
        Transform container = currentInstance.transform.Find("LiquidContainer");
        if (container == null)
        {
            Debug.LogError("LiquidContainer が 見つかりません");
            return;
        }

        Transform lp = container.Find("LiquidParticle");
        Transform ls = container.Find("LiquidSurface");

        if (lp != null)
            liquidParticle = lp.GetComponent<ParticleSystem>();
        else
            Debug.LogError("LiquidParticle が 見つかりません");

        if (ls != null)
            liquidSurface = ls.GetComponent<Renderer>();
        else
            Debug.LogError("LiquidSurface が 見つかりません");

        // ================================
        // 各種セットアップ
        // ================================
        ConfigureLiquidParticle(mf);
        FixRenderingOrder(mr);
=======
            Debug.LogError("Model に MeshFilter または MeshRenderer がありません");
            return;
        }

        // -------------------------
        // LiquidContainer
        // -------------------------
        Transform container = flask.transform.Find("LiquidContainer");
        if (container != null)
        {
            Transform lp = container.Find("LiquidParticle");
            if (lp != null)
                liquidParticle = lp.GetComponent<ParticleSystem>();

            Transform ls = container.Find("LiquidSurface");
            if (ls != null)
                liquidSurface = ls.GetComponent<Renderer>();
        }

        // 必須初期化（引数つき）
        ConfigureLiquidParticle(mf);
        FixRenderingOrder(mr);
        InjectSurfaceReferences(); // ← 引数不要、そのままで OK
    }

    private Vector3 GetValidRandomPosition()
    {
        Vector3 areaCenter = spawnArea.transform.position + spawnArea.center;
        Vector3 areaSize = spawnArea.size;

        const int MAX_TRY = 50;

        for (int i = 0; i < MAX_TRY; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-areaSize.x / 2, areaSize.x / 2),
                Random.Range(-areaSize.y / 2, areaSize.y / 2),
                Random.Range(-areaSize.z / 2, areaSize.z / 2)
            );

            Vector3 pos = areaCenter + randomOffset;

            // 既存フラスコとの距離チェック
            bool ok = true;

            for (int j = 0; j < flaskCount; j++)
            {
                if (flaskList[j] == null) continue;

                float dist = Vector3.Distance(pos, flaskList[j].transform.position);
                if (dist < minDistance)
                {
                    ok = false;
                    break;
                }
            }

            if (ok) return pos;
        }

        // 失敗したら中心に置く
        return spawnArea.transform.position;
    }

    private void InjectSurfaceReferences()
    {
        if (currentInstance == null) return;

        // --- LiquidContainer ---
        Transform container = currentInstance.transform.Find("LiquidContainer");
        if (container == null)
        {
            Debug.LogError("InjectSurfaceReferences: LiquidContainer が見つかりません");
            return;
        }

        // --- LiquidSurface ---
        Transform ls = container.Find("LiquidSurface");
        if (ls == null)
        {
            Debug.LogError("InjectSurfaceReferences: LiquidSurface が見つかりません");
            return;
        }

        MeshRenderer surfaceRenderer = ls.GetComponent<MeshRenderer>();
        if (surfaceRenderer == null)
        {
            Debug.LogError("InjectSurfaceReferences: LiquidSurface に MeshRenderer がありません");
            return;
        }

        // --- LiquidParticle ---
        Transform lp = container.Find("LiquidParticle");
        if (lp == null)
        {
            Debug.LogError("InjectSurfaceReferences: LiquidParticle が見つかりません");
            return;
        }

        ParticleSystem particle = lp.GetComponent<ParticleSystem>();
        if (particle == null)
        {
            Debug.LogError("InjectSurfaceReferences: LiquidParticle に ParticleSystem がありません");
            return;
        }

        // =========================
        // LiquidSurfaceController
        // =========================
        var lsc = ls.GetComponent<UdonSharpBehaviour>();
        if (lsc != null)
        {
            if (lsc.GetProgramVariable("surfaceRenderer") != null)
                lsc.SetProgramVariable("surfaceRenderer", surfaceRenderer);
>>>>>>> Stashed changes

            if (lsc.GetProgramVariable("particle") != null)
                lsc.SetProgramVariable("particle", particle);
        }

        // =========================
        // LiquidEffects 全てに注入
        // =========================
        Transform effectsRoot = container.Find("LiquidEffects");
        if (effectsRoot == null)
        {
            Debug.LogError("InjectSurfaceReferences: LiquidEffects が見つかりません");
            return;
        }

        UdonSharpBehaviour[] effects = effectsRoot.GetComponents<UdonSharpBehaviour>();

        foreach (var ef in effects)
        {
            // Surface 変数
            if (ef.GetProgramVariable("Surface") != null)
                ef.SetProgramVariable("Surface", ls.gameObject);

            // Wave
            if (ef.GetProgramVariable("Wave") != null)
                ef.SetProgramVariable("Wave", ls.gameObject);

            // Boil
            if (ef.GetProgramVariable("Boil") != null)
                ef.SetProgramVariable("Boil", ls.gameObject);

            // ParticleEngine
            if (ef.GetProgramVariable("ParticleEngine") != null)
                ef.SetProgramVariable("ParticleEngine", particle);
        }

        Debug.Log("InjectSurfaceReferences: 参照注入完了");
    }

    private void InjectSurfaceReferences()
    {
        Transform container = currentInstance.transform.Find("LiquidContainer");
        Transform ls = container.Find("LiquidSurface");
        Transform lp = container.Find("LiquidParticle");
        MeshRenderer surfaceRend = ls.GetComponent<MeshRenderer>();
        ParticleSystem particle = lp.GetComponent<ParticleSystem>();

        // ---- LiquidSurfaceController ----
        var lsc = ls.GetComponent<UdonSharpBehaviour>();
        if (lsc != null)
        {
            lsc.SetProgramVariable("surfaceRenderer", surfaceRend);
        }

        // ---- LiquidEffects 配下全入れ ----
        Transform leRoot = container.Find("LiquidEffects");
        var effects = leRoot.GetComponents<UdonSharpBehaviour>();
        foreach (var ef in effects)
        {
            // Surface
            ef.SetProgramVariable("Surface", ls.gameObject);

            // Wave / Boil / ParticleEngine など存在する変数にだけ注入
            if (ef.GetProgramVariable("Wave") != null)
                ef.SetProgramVariable("Wave", ls.gameObject);

            if (ef.GetProgramVariable("Boil") != null)
                ef.SetProgramVariable("Boil", ls.gameObject);

            if (ef.GetProgramVariable("ParticleEngine") != null)
                ef.SetProgramVariable("ParticleEngine", particle);
        }
    }

    // ============================================================
    // Particle 設定（Mesh 内に閉じ込める & 初期非表示）
    // ============================================================
    private void ConfigureLiquidParticle(MeshFilter modelMesh)
    {
        if (liquidParticle == null) return;
        if (modelMesh == null) return;

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

        // Model の Mesh を渡す
        shape.mesh = modelMesh.sharedMesh;

        shape.normalOffset = -0.02f;

        ParticleSystemRenderer pr = liquidParticle.GetComponent<ParticleSystemRenderer>();
        pr.material.renderQueue = 2700;
        pr.material.SetInt("_ZWrite", 0);
    }

    // ============================================================
    // 描画順序：Wireframe → Surface → Particle
    // ============================================================
    private void FixRenderingOrder(MeshRenderer modelRenderer)
    {
        Material wireMat = modelRenderer.material;
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
<<<<<<< Updated upstream
    // 振ったときの揺れ
=======
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

    // Solid: 暗くする
    private Color GetSolidColor(Color baseColor)
    {
        return baseColor * 0.55f;
    }

    // Liquid: そのまま
    private Color GetLiquidColor(Color baseColor)
    {
        return baseColor;
    }

    // Gas: 明るく / 透明感 / 白寄り
    private Color GetGasColor(Color baseColor)
    {
        return Color.Lerp(baseColor, Color.white, 0.5f);
    }
    private Color GetStateColor(ElementData data, float temp)
    {
        Color solid = GetSolidColor(data.color);
        Color liquid = GetLiquidColor(data.color);
        Color gas = GetGasColor(data.color);

        if (temp < data.meltingPoint)
            return solid;

        // Solid ⇢ Liquid ⇢ Gas の間を補間
        if (temp < data.boilingPoint)
        {
            float t = Mathf.InverseLerp(data.meltingPoint, data.boilingPoint, temp);
            return Color.Lerp(liquid, gas, t);
        }

        // 気体
        return gas;
    }

    private void ApplyAppearance(ElementData data)
    {
        float temp = environment.Temperature;

        Color c = GetStateColor(data, temp);

        if (liquidSurface != null)
            liquidSurface.material.SetColor("_Color", c);

        if (liquidParticle != null)
        {
            var main = liquidParticle.main;
            main.startColor = c;
        }
    }

    // ============================================================
    // SHAKE NOISE + STATE UPDATE
>>>>>>> Stashed changes
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
