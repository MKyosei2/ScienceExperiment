using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    // =====================================
    //  Public Externally Set Values
    // =====================================
    public Transform spawnParent;
    public GameObject sourceVessel;

    // 内部液体表現（パーティクル）
    public ParticleSystem liquidParticles;

    // 溢れパーティクル
    public GameObject overflowParticlePrefab;
    public int overflowPoolSize = 10;

    // =====================================
    //  Internal State
    // =====================================
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string bondData = "";

    private GameObject currentInstance;
    private ParticleSystem[] overflowPool;

    private float fillAmount = 0f;
    private float maxFill = 1.0f;

    //----------------------------------------
    void Start()
    {
        InitOverflow();
    }

    //----------------------------------------
    private void InitOverflow()
    {
        overflowPool = new ParticleSystem[overflowPoolSize];
        for (int i = 0; i < overflowPoolSize; i++)
        {
            GameObject p = VRCInstantiate(overflowParticlePrefab);
            p.transform.SetParent(spawnParent, false);
            p.SetActive(false);
            overflowPool[i] = p.GetComponent<ParticleSystem>();
        }
    }

    // ============================================
    //   External API (SpawnSelectorButton → ここ)
    // ============================================
    public void SelectElement(string n)
    {
        selectedElementName = n;

        // Vessel を作成
        SpawnFlask();

        // 内部粒子の挙動を更新
        ApplyElementBehavior(n);
    }

    public void SelectEquipment(string n)
    {
        selectedEquipmentName = n;
        SpawnFlask();
    }

    public void StartExperiment() { }
    public void ResetExperiment() { ResetAll(); }
    public void ApplyBondUpdate() { }

    // ============================================
    //   Spawn Flask
    // ============================================
    private void SpawnFlask()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        // ==== 1. Scene の SpawnPoint を取得 ====
        Transform spawnPoint = GameObject.Find("SpawnPoint").transform;

        if (spawnPoint == null)
        {
            Debug.LogError("[Spawner] SpawnPoint が Scene にありません。");
            return;
        }

        // ==== 2. インスタンス生成（親をつけない！）====
        currentInstance = VRCInstantiate(sourceVessel);

        // ==== 3. 生成位置を完全固定 ====
        currentInstance.transform.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        // スケールも完全一致
        currentInstance.transform.localScale = spawnPoint.lossyScale;

        // ==== 4. 親を絶対に設定しない（重要）====
        // currentInstance.transform.SetParent(spawnParent); ← 使わない！

        // ==== 5. 内部液体をセット ====
        SetupLiquidParticles();
    }

    private void SetupLiquidParticles()
    {
        if (liquidParticles == null) return;
        if (currentInstance == null) return;

        // フラスコ内部中央に配置
        liquidParticles.transform.SetParent(currentInstance.transform, false);
        liquidParticles.transform.localPosition = new Vector3(0, 0.4f, 0);
        liquidParticles.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    }

    // ============================================
    //   Reset
    // ============================================
    private void ResetAll()
    {
        if (currentInstance != null)
            Destroy(currentInstance);

        if (liquidParticles != null)
            liquidParticles.Stop();

        fillAmount = 0f;
        selectedElementName = "";
        selectedEquipmentName = "";
        bondData = "";
    }

    // ============================================
    //   Apply Behavior for Element (方式D)
    // ============================================
    public void ApplyElementBehavior(string element)
    {
        if (liquidParticles == null)
        {
            Debug.LogWarning("[Spawner] liquidParticles が設定されていません。");
            return;
        }

        var main = liquidParticles.main;
        var emission = liquidParticles.emission;

        //-------------------------------------
        // 色設定（118元素に対応）
        //-------------------------------------
        Color col = GetColorForElement(element);
        col.a = 0.7f;
        main.startColor = col;

        //-------------------------------------
        // 元素ごとの粒子挙動（方式D）
        //-------------------------------------
        switch (element)
        {
            // ===========================
            // 金属系（重く沈殿）
            // ===========================
            case "Rh":
            case "Fe":
            case "Cu":
            case "Ag":
            case "Au":
                main.startSize = 0.045f;
                main.startSpeed = 0.05f;
                main.gravityModifier = 0.3f;
                emission.rateOverTime = 160;
                break;

            // ===========================
            // 気体（軽く上昇）
            // ===========================
            case "H":
            case "He":
            case "Ne":
            case "Ar":
            case "Kr":
            case "Xe":
                main.startSize = 0.08f;
                main.startSpeed = 0.4f;
                main.gravityModifier = -0.25f;
                emission.rateOverTime = 50;
                break;

            // ===========================
            // ハロゲン（発光気味）
            // ===========================
            case "F":
            case "Cl":
            case "Br":
            case "I":
                main.startSize = 0.06f;
                main.startSpeed = 0.15f;
                main.gravityModifier = -0.05f;
                main.startColor = col * 1.3f;
                emission.rateOverTime = 130;
                break;

            // ===========================
            // その他の気体/液体
            // ===========================
            case "O":
            case "N":
                main.startSize = 0.05f;
                main.startSpeed = 0.2f;
                main.gravityModifier = 0.0f;
                emission.rateOverTime = 100;
                break;

            // ===========================
            // 汎用
            // ===========================
            default:
                main.startSize = 0.05f;
                main.startSpeed = 0.2f;
                main.gravityModifier = 0.0f;
                emission.rateOverTime = 120;
                break;
        }

        //-------------------------------------
        // 再生
        //-------------------------------------
        liquidParticles.Play();
    }

    // ============================================
    //   Overflow
    // ============================================
    public void AddAmount(float amt)
    {
        fillAmount += amt;
        if (fillAmount > maxFill)
        {
            float overflow = fillAmount - maxFill;
            PlayOverflow(overflow);
        }
    }

    private void PlayOverflow(float strength)
    {
        if (currentInstance == null) return;

        foreach (var ps in overflowPool)
        {
            if (!ps.gameObject.activeSelf)
            {
                ps.gameObject.SetActive(true);

                var main = ps.main;
                main.startColor = GetColorForElement(selectedElementName);

                ps.transform.position =
                    currentInstance.transform.position + new Vector3(0, 0.8f, 0);

                ps.Play();
                return;
            }
        }
    }

    // ============================================
    //   118元素 → 色
    // ============================================
    private Color GetColorForElement(string e)
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

            // （略：ここまでで全元素入っている）

            default: return RGB(180, 180, 180);
        }
    }

    private Color RGB(byte r, byte g, byte b)
    {
        return new Color32(r, b, g, 255);
    }
}
