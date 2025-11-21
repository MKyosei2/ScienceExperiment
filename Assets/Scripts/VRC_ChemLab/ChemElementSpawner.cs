using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string bondData = "";

    [Header("=== Spawn Settings ===")]
    public Transform spawnParent;
    public GameObject sourceVessel;

    private GameObject currentInstance;
    private ParticleSystem insideParticle;

    // 元素履歴（最大4つ）
    private string[] selectedElements = new string[4];
    private int elementCount = 0;

    [Header("=== Environment ===")]
    public float temperature = 25f;
    public float pressure = 1f;

    [Header("=== Overflow ===")]
    public GameObject overflowParticlePrefab; // ← ここに CONICAL_FLASK をそのまま入れてOK!!
    private Vector3 overflowOffset = new Vector3(0f, 0.02f, 0f);

    private bool hasElementSelected = false;

    void Start() { }

    // ===========================================================
    // 外部 API
    // ===========================================================
    public void SelectElement(string n)
    {
        selectedElementName = n;
        SendCustomEvent("_SelectElement");
    }

    public void SelectEquipment(string n)
    {
        selectedEquipmentName = n;
        SendCustomEvent("_SelectEquipment");
    }

    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }

    // ===========================================================
    // UI Event
    // ===========================================================
    public void _SelectElement()
    {
        hasElementSelected = true;
        SpawnFlask();
        ApplyElementParticleColor();

        selectedElements[elementCount] = selectedElementName;
        elementCount++;
    }

    public void _SelectEquipment()
    {
        SpawnFlask();
        if (hasElementSelected)
            ApplyElementParticleColor();
    }

    public void _ResetExperiment()
    {
        if (currentInstance != null) Destroy(currentInstance);

        hasElementSelected = false;
        selectedElementName = "";
        selectedEquipmentName = "";
        bondData = "";
        elementCount = 0;
    }

    // ===========================================================
    // Flask Spawn
    // ===========================================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        currentInstance = VRCInstantiate(sourceVessel);
        currentInstance.transform.SetParent(spawnParent, true);

        // 内部パーティクル取得
        insideParticle = currentInstance.transform.Find("Particle").GetComponent<ParticleSystem>();

        // ==========================================
        // ① WireframeFX（Model）の描画順を修正
        // ==========================================
        MeshRenderer wireMR = currentInstance.transform.Find("Model").GetComponent<MeshRenderer>();
        if (wireMR != null)
        {
            Material wireMat = wireMR.material;
            wireMat.renderQueue = 3100;  // 手前
            wireMat.SetInt("_ZWrite", 0); // 深度を書かない
        }

        // ==========================================
        // ② 液体 Particle の描画順を修正
        // ==========================================
        ParticleSystemRenderer pr = insideParticle.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Material liquidMat = pr.material;
            liquidMat.renderQueue = 3000;       // 透明
            liquidMat.SetInt("_ZWrite", 0);     // 深度を書かない

            pr.sortingOrder = 10;               // Wireframe より後ろ
        }

        // ==========================================
        // ③ 遠距離カリングを防ぐ（Bounds拡大）
        // ==========================================
        MeshRenderer[] renderers = currentInstance.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            r.localBounds = new Bounds(Vector3.zero, new Vector3(9999, 9999, 9999));
        }
    }

    // ===========================================================
    // Element → Color
    // ===========================================================
    private void ApplyElementParticleColor()
    {
        if (insideParticle == null) return;

        var main = insideParticle.main;
        main.startColor = GetColorFromElement(selectedElementName);
    }

    private Color GetCurrentLiquidColor()
    {
        if (insideParticle == null) return Color.white;
        return insideParticle.main.startColor.color;
    }

    // ===========================================================
    // Add Amount → Overflow
    // ===========================================================
    public void AddAmount(float amt)
    {
        if (insideParticle == null) return;

        var em = insideParticle.emission;
        float newRate = em.rateOverTime.constant + amt * 80f;
        newRate = Mathf.Clamp(newRate, 0f, 500f);
        em.rateOverTime = newRate;

        if (newRate > 350f)
        {
            float overflow = (newRate - 350f) / 60f;
            PlayOverflow(overflow);
        }
    }

    // ===========================================================
    // Overflow Fix（CONICAL_FLASK を入れても無限増殖しない）
    // ===========================================================
    private void PlayOverflow(float strength)
    {
        if (currentInstance == null) return;

        // --- CONICAL_FLASK 内の "Particle" GameObject を探す ---
        GameObject particleObj = null;

        if (overflowParticlePrefab != null)
        {
            Transform p = overflowParticlePrefab.transform.Find("Particle");
            if (p != null)
                particleObj = p.gameObject;
        }

        if (particleObj == null)
        {
            Debug.LogError("❌ Overflow particle GameObject not found inside CONICAL_FLASK!");
            return;
        }

        // --- Udon対応：Instantiate は GameObject のみ可能 ---
        GameObject psObj = VRCInstantiate(particleObj);
        psObj.transform.SetParent(spawnParent, true);

        ParticleSystem ps = psObj.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError("❌ Instantiated overflow object has no ParticleSystem!");
            return;
        }

        // --- 色を内部液体と同じにする ---
        var main = ps.main;
        if (insideParticle != null)
            main.startColor = insideParticle.main.startColor;

        // --- フラスコ上端に配置 ---
        MeshRenderer mr = currentInstance.GetComponentInChildren<MeshRenderer>();
        Bounds b = mr.bounds;
        Vector3 pos = new Vector3(b.center.x, b.max.y + 0.02f, b.center.z);
        psObj.transform.position = pos;

        // --- 強度調整 ---
        var em = ps.emission;
        em.rateOverTime = 6f * strength;

        ps.Play();
    }

    // ===========================================================
    // 118元素 → 色
    // ===========================================================
    private Color GetColorFromElement(string e)
    {
        switch (e)
        {
            case "H": return RGB(240, 240, 240);
            case "He": return RGB(235, 245, 255);

            case "Li": return RGB(180, 180, 190);
            case "Be": return RGB(196, 201, 206);
            case "B": return RGB(80, 80, 80);
            case "C": return RGB(30, 30, 30);
            case "N": return RGB(220, 230, 255);
            case "O": return RGB(180, 210, 255);
            case "F": return RGB(202, 255, 112);
            case "Ne": return RGB(255, 90, 60);

            case "Na": return RGB(250, 230, 130);
            case "Mg": return RGB(190, 190, 195);
            case "Al": return RGB(210, 210, 215);
            case "Si": return RGB(90, 90, 95);
            case "P": return RGB(255, 255, 255);
            case "S": return RGB(255, 240, 70);
            case "Cl": return RGB(205, 255, 112);
            case "Ar": return RGB(210, 230, 255);

            case "K": return RGB(160, 140, 115);
            case "Ca": return RGB(200, 200, 200);
            case "Sc": return RGB(190, 190, 200);
            case "Ti": return RGB(185, 190, 195);
            case "V": return RGB(170, 175, 180);
            case "Cr": return RGB(200, 200, 205);
            case "Mn": return RGB(180, 180, 185);
            case "Fe": return RGB(170, 170, 170);
            case "Co": return RGB(170, 175, 185);
            case "Ni": return RGB(185, 185, 190);
            case "Cu": return RGB(198, 120, 70);
            case "Zn": return RGB(200, 200, 205);
            case "Ga": return RGB(210, 210, 220);
            case "Ge": return RGB(105, 105, 110);
            case "As": return RGB(145, 140, 150);
            case "Se": return RGB(150, 40, 40);
            case "Br": return RGB(150, 40, 0);
            case "Kr": return RGB(220, 235, 255);

            case "Rb": return RGB(170, 145, 125);
            case "Sr": return RGB(220, 220, 230);
            case "Y": return RGB(195, 200, 210);
            case "Zr": return RGB(200, 200, 210);
            case "Nb": return RGB(175, 180, 190);
            case "Mo": return RGB(185, 190, 200);
            case "Tc": return RGB(160, 165, 175);
            case "Ru": return RGB(195, 200, 210);
            case "Rh": return RGB(200, 205, 215);
            case "Pd": return RGB(200, 205, 210);
            case "Ag": return RGB(230, 230, 235);
            case "Cd": return RGB(210, 210, 220);
            case "In": return RGB(210, 215, 225);
            case "Sn": return RGB(195, 200, 210);
            case "Sb": return RGB(170, 175, 185);
            case "Te": return RGB(95, 100, 110);
            case "I": return RGB(80, 0, 120);
            case "Xe": return RGB(200, 220, 255);

            case "Cs": return RGB(170, 150, 120);
            case "Ba": return RGB(210, 220, 230);
            case "La": return RGB(200, 205, 215);
            case "Ce": return RGB(190, 195, 205);
            case "Pr": return RGB(190, 195, 205);
            case "Nd": return RGB(185, 190, 200);
            case "Pm": return RGB(180, 185, 195);
            case "Sm": return RGB(190, 195, 205);
            case "Eu": return RGB(245, 245, 255);
            case "Gd": return RGB(190, 195, 205);
            case "Tb": return RGB(190, 195, 205);
            case "Dy": return RGB(190, 195, 205);
            case "Ho": return RGB(190, 195, 205);
            case "Er": return RGB(190, 195, 205);
            case "Tm": return RGB(190, 195, 205);
            case "Yb": return RGB(230, 235, 245);
            case "Lu": return RGB(195, 200, 210);

            case "Hf": return RGB(190, 195, 205);
            case "Ta": return RGB(115, 120, 130);
            case "W": return RGB(150, 150, 160);
            case "Re": return RGB(140, 140, 150);
            case "Os": return RGB(130, 135, 145);
            case "Ir": return RGB(200, 205, 215);
            case "Pt": return RGB(210, 210, 220);
            case "Au": return RGB(212, 175, 55);
            case "Hg": return RGB(210, 210, 220);

            case "Tl": return RGB(160, 165, 175);
            case "Pb": return RGB(125, 130, 140);
            case "Bi": return RGB(190, 195, 210);
            case "Po": return RGB(140, 140, 150);
            case "At": return RGB(100, 90, 110);
            case "Rn": return RGB(220, 230, 245);

            case "Fr": return RGB(180, 170, 160);
            case "Ra": return RGB(220, 230, 230);

            case "Ac": return RGB(170, 175, 185);
            case "Th": return RGB(180, 185, 195);
            case "Pa": return RGB(90, 95, 105);
            case "U": return RGB(70, 90, 40);
            case "Np": return RGB(100, 105, 115);
            case "Pu": return RGB(110, 115, 125);
            case "Am": return RGB(130, 135, 145);
            case "Cm": return RGB(150, 155, 165);
            case "Bk": return RGB(160, 165, 175);
            case "Cf": return RGB(170, 175, 185);
            case "Es": return RGB(180, 185, 195);
            case "Fm": return RGB(185, 190, 200);
            case "Md": return RGB(190, 195, 205);
            case "No": return RGB(195, 200, 210);
            case "Lr": return RGB(200, 205, 215);

            case "Rf": return RGB(180, 185, 195);
            case "Db": return RGB(180, 185, 195);
            case "Sg": return RGB(180, 185, 195);
            case "Bh": return RGB(180, 185, 195);
            case "Hs": return RGB(180, 185, 195);
            case "Mt": return RGB(180, 185, 195);
            case "Ds": return RGB(180, 185, 195);
            case "Rg": return RGB(210, 190, 120);
            case "Cn": return RGB(180, 185, 195);
            case "Nh": return RGB(180, 185, 195);
            case "Fl": return RGB(180, 185, 195);
            case "Mc": return RGB(180, 185, 195);
            case "Lv": return RGB(180, 185, 195);
            case "Ts": return RGB(180, 185, 195);
            case "Og": return RGB(220, 230, 245);
        }

        return RGB(180, 180, 180);
    }

    private Color RGB(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    // ===========================================================
    // 混色（AI 未定義反応用）
    // ===========================================================
    private Color MixColor(Color a, Color b)
    {
        return new Color(
            (a.r + b.r) * 0.5f,
            (a.g + b.g) * 0.5f,
            (a.b + b.b) * 0.5f
        );
    }
}
