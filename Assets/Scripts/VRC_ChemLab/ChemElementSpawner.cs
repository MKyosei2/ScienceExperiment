using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemElementDatabase db;
    public ReactionPredictor predictor;
    public ChemReactionAnimator animator;

    public Transform spawnParent;
    public GameObject sourceVessel;
    public GameObject overflowParticlePrefab;

    public float temperature = 25f;
    public float humidity = 50f;
    public float pressure = 1f;

    private GameObject currentInstance;
    private ParticleSystem insideParticle;

    private string lastElement = "";

    private Vector3 lastPos;
    private Quaternion lastRot;

    // ---- 旧API互換用 ----
    public string selectedElementName = "";
    public string selectedEquipmentName = "";

    // =========================================================
    public void SelectElement(string symbol)
    {
        selectedElementName = symbol;

        SpawnFlask();

        lastElement = symbol;

        Color32 baseCol = db.GetColor(symbol);
        ApplyAppearance(baseCol, symbol);
    }

    public void SelectEquipment(string name)
    {
        selectedEquipmentName = name;
    }

    // =========================================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);
        currentInstance.transform.SetParent(spawnParent, true);

        insideParticle = currentInstance.transform.Find("Particle").GetComponent<ParticleSystem>();

        FixRenderingOrder();

        lastPos = currentInstance.transform.position;
        lastRot = currentInstance.transform.rotation;
    }

    // =========================================================
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
            r.localBounds = new Bounds(Vector3.zero, Vector3.one * 9999f);
    }

    // =========================================================
    private void ApplyAppearance(Color32 baseCol32, string element)
    {
        Color col = (Color)baseCol32;

        float t = Mathf.Lerp(0.8f, 1.2f, temperature / 100f);
        col *= t;

        col.a = Mathf.Lerp(0.4f, 1f, humidity / 100f);

        var main = insideParticle.main;
        main.startColor = col;

        float vis = db.GetViscosity(element);
        main.startSpeed = Mathf.Clamp(1f / (vis * pressure), 0.1f, 1.2f);

        var noise = insideParticle.noise;
        noise.enabled = true;
        noise.strength = db.GetDensity(element) * 0.2f;
    }

    // =========================================================
    // VR 手振りで液体揺れ
    // =========================================================
    private void Update()
    {
        if (currentInstance == null || insideParticle == null) return;

        Vector3 pos = currentInstance.transform.position;
        float move = (pos - lastPos).magnitude * 30f;

        Quaternion rot = currentInstance.transform.rotation;
        float rotSpeed = Quaternion.Angle(rot, lastRot) * 0.2f;

        float shake = Mathf.Clamp(move + rotSpeed, 0f, 3f);

        // ノイズ揺れ適用
        var noise = insideParticle.noise;
        noise.enabled = true;

        // 旧 `/=` のエラー修正版
        float rawStrength = 0.2f + shake * 0.4f;
        float visc = db.GetViscosity(selectedElementName);
        float finalStrength = rawStrength / Mathf.Clamp(visc, 0.5f, 2f);

        noise.strength = finalStrength;
        noise.frequency = 0.5f + shake * 1.5f;

        lastPos = pos;
        lastRot = rot;
    }

    // =========================================================
    // 化学反応
    // =========================================================
    public void CombineWith(string next)
    {
        string reaction = predictor.Predict(lastElement, next);

        Debug.Log("[ChemLab Reaction] " + reaction);

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
