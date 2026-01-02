#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ChemLabSetupWizard
{
    private const string Root = "Assets/ChemLabGenerated";
    private const string ShaderFolder = "Assets/Shaders";
    private const string MatFolder = Root + "/Materials";
    private const string PrefabFolder = Root + "/Prefabs";

    [MenuItem("ChemLab/1) Create Materials (Solid/Liquid/Gas/Particle)")]
    public static void CreateMaterials()
    {
        EnsureFolders();

        CreateMat("ChemLab_Solid.mat", "ChemLab/Solid", (m) =>
        {
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Opacity", 0.98f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.4f);
            m.SetFloat("_NoiseScale", 0.28f);
            m.SetFloat("_EmissionStrength", 0f);
            m.SetFloat("_Dissolve", 0f);
        });

        CreateMat("ChemLab_Liquid.mat", "ChemLab/Liquid", (m) =>
        {
            m.SetColor("_BaseColor", new Color(0.7f, 0.9f, 1f, 1f));
            m.SetFloat("_Opacity", 0.12f);
            m.SetFloat("_Smoothness", 0.95f);
            m.SetFloat("_NoiseScale", 0.18f);
            m.SetFloat("_EmissionStrength", 0f);
            m.SetFloat("_Dissolve", 0f);
            m.SetFloat("_Viscosity", 1f);
            m.SetFloat("_Density", 1f);
            m.SetFloat("_Glow", 0f);
            m.SetFloat("_WaveStrength", 0f);
        });

        CreateMat("ChemLab_Gas.mat", "ChemLab/GasFog", (m) =>
        {
            m.SetColor("_BaseColor", new Color(0.85f, 0.95f, 1f, 1f));
            m.SetFloat("_Opacity", 0.08f);
            m.SetFloat("_NoiseScale", 0.6f);
            m.SetFloat("_EmissionStrength", 0f);
            m.SetFloat("_Dissolve", 0f);
            m.SetFloat("_FogSoftness", 1.5f);
        });

        CreateMat("ChemLab_Particle.mat", "ChemLab/ParticleUnlit", (m) =>
        {
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Opacity", 1f);
            m.SetFloat("_SoftFactor", 1f);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChemLab] Materials created under " + MatFolder);
    }

    [MenuItem("ChemLab/2) Create VFX Preset Prefab (Glint/Precipitate/Bubble/Fog)")]
    public static void CreateVfxPresetPrefab()
    {
        EnsureFolders();

        var particleMat = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/ChemLab_Particle.mat");
        if (particleMat == null)
        {
            Debug.LogWarning("[ChemLab] Particle material not found. Run 'Create Materials' first.");
            return;
        }

        var root = new GameObject("ChemLab_VFX_Presets");

        CreateParticleChild(root.transform, "Glint", particleMat, shapeCone: true, rate: 8f, startSize: 0.03f, lifetime: 0.6f, speed: 0.2f);
        CreateParticleChild(root.transform, "Precipitate", particleMat, shapeCone: false, rate: 40f, startSize: 0.02f, lifetime: 1.2f, speed: 0.1f);
        CreateParticleChild(root.transform, "Bubble", particleMat, shapeCone: false, rate: 30f, startSize: 0.04f, lifetime: 0.9f, speed: 0.25f);
        CreateParticleChild(root.transform, "Fog", particleMat, shapeCone: true, rate: 18f, startSize: 0.18f, lifetime: 1.6f, speed: 0.12f);

        string path = PrefabFolder + "/ChemLab_VFX_Presets.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChemLab] VFX preset prefab created: " + path);
    }

    [MenuItem("ChemLab/3) Assign VFX Presets to Selected ChemReactionAnimator")]
    public static void AssignToSelectedAnimator()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("[ChemLab] Select a GameObject that has ChemReactionAnimator.");
            return;
        }

        var anim = go.GetComponent<ChemReactionAnimator>();
        if (anim == null)
        {
            Debug.LogWarning("[ChemLab] Selected object has no ChemReactionAnimator.");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ChemLab_VFX_Presets.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[ChemLab] VFX preset prefab not found. Run 'Create VFX Preset Prefab' first.");
            return;
        }

        // Instantiate under animator
        var inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        inst.transform.SetParent(go.transform, false);

        anim.glintParticles = inst.transform.Find("Glint")?.GetComponent<ParticleSystem>();
        anim.precipitateParticles = inst.transform.Find("Precipitate")?.GetComponent<ParticleSystem>();
        anim.bubbleParticles = inst.transform.Find("Bubble")?.GetComponent<ParticleSystem>();
        anim.fogParticles = inst.transform.Find("Fog")?.GetComponent<ParticleSystem>();

        EditorUtility.SetDirty(anim);
        Debug.Log("[ChemLab] Assigned VFX presets to ChemReactionAnimator on: " + go.name);
    }

    [MenuItem("ChemLab/4) Populate 12 Starter Known Compounds on Selected ChemElementDatabase")]
    public static void PopulateStarterCompoundsOnSelectedDatabase()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("[ChemLab] Select a GameObject that has ChemElementDatabase.");
            return;
        }

        var db = go.GetComponent<ChemElementDatabase>();
        if (db == null)
        {
            Debug.LogWarning("[ChemLab] Selected object has no ChemElementDatabase.");
            return;
        }

        // 12 starter set (school/science museum friendly)
        var rows = new List<Row>
        {
            Row.Make("H2O", "水", Hex("AEEBFF"), 0f, 100f, 0, 3, 0, 0.12f, 0f, 0.92f, 0.00f, 0.15f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("NaCl", "塩化ナトリウム", Hex("F4F6FF"), 801f, 1413f, 0, 0, 1, 0.95f, 0f, 0.35f, 0.02f, 0.30f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("CuSO4·5H2O", "硫酸銅(II)五水和物", Hex("1E6BFF"), 110f, 650f, 0, 0, 1, 0.98f, 0f, 0.55f, 0.10f, 0.25f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("KMnO4", "過マンガン酸カリウム", Hex("6A00FF"), 240f, 240f, 0, 0, 1, 0.98f, 0f, 0.45f, 0.08f, 0.25f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("NaOH", "水酸化ナトリウム", Hex("FFFFFF"), 318f, 1388f, 0, 2, 0, 0.97f, 0f, 0.25f, 0.00f, 0.20f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("CaCO3", "炭酸カルシウム", Hex("FFFFFF"), 825f, 825f, 0, 1, 0, 0.98f, 0f, 0.15f, 0.00f, 0.55f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("NaHCO3", "炭酸水素ナトリウム", Hex("FFFFFF"), 50f, 100f, 0, 1, 3, 0.98f, 0f, 0.10f, 0.00f, 0.60f, 0f, 0.65f, 1.0f, 1.0f),
            Row.Make("H2O2", "過酸化水素", Hex("D8F0FF"), -0.43f, 150.2f, 0, 3, 0, 0.10f, 0f, 0.90f, 0.02f, 0.18f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("I2", "ヨウ素", Hex("1B1028"), 113.7f, 184.4f, 0, 0, 4, 0.98f, 0.05f, 0.40f, 0.06f, 0.25f, 0.55f, 0f, 1.0f, 1.0f),
            Row.Make("S8", "硫黄", Hex("FFD400"), 115.2f, 444.6f, 0, 0, 0, 0.98f, 0f, 0.35f, 0.03f, 0.22f, 0f, 0f, 1.0f, 1.0f),
            Row.Make("CO2", "二酸化炭素", Hex("CFE8FF"), -56.6f, -78.5f, 0, 4, 4, 0.08f, 0f, 0.00f, 0.00f, 0.35f, 0.75f, 0f, 1.0f, 1.0f),
            Row.Make("NH3", "アンモニア", Hex("E6FFF8"), -77.7f, -33.34f, 0, 4, 4, 0.06f, 0f, 0.00f, 0.00f, 0.40f, 0.65f, 0f, 1.0f, 1.0f),
        };

        int n = rows.Count;
        db.CompoundFormulas = new string[n];
        db.CompoundNamesJa = new string[n];
        db.CompoundDisplayColors = new Color[n];
        db.CompoundMeltingPointC = new float[n];
        db.CompoundBoilingPointC = new float[n];
        db.CompoundHazardFlags = new int[n];

        db.CompoundArchetype = new int[n];
        db.CompoundParticlePreset = new int[n];
        db.CompoundOpacity = new float[n];
        db.CompoundMetallic = new float[n];
        db.CompoundSmoothness = new float[n];
        db.CompoundEmission = new float[n];
        db.CompoundNoiseScale = new float[n];
        db.CompoundFogDensity = new float[n];
        db.CompoundBubbleRate = new float[n];
        db.CompoundViscosity = new float[n];
        db.CompoundDensity = new float[n];

        for (int i = 0; i < n; i++)
        {
            var r = rows[i];
            db.CompoundFormulas[i] = r.formula;
            db.CompoundNamesJa[i] = r.name;
            db.CompoundDisplayColors[i] = r.color;
            db.CompoundMeltingPointC[i] = r.mp;
            db.CompoundBoilingPointC[i] = r.bp;
            db.CompoundHazardFlags[i] = r.hazard;

            db.CompoundArchetype[i] = r.arch;
            db.CompoundParticlePreset[i] = r.preset;
            db.CompoundOpacity[i] = r.opacity;
            db.CompoundMetallic[i] = r.metal;
            db.CompoundSmoothness[i] = r.smooth;
            db.CompoundEmission[i] = r.emiss;
            db.CompoundNoiseScale[i] = r.noise;
            db.CompoundFogDensity[i] = r.fog;
            db.CompoundBubbleRate[i] = r.bubble;
            db.CompoundViscosity[i] = r.visc;
            db.CompoundDensity[i] = r.dens;
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("[ChemLab] Populated starter known compounds on: " + go.name);
    }

    // ---------------- helpers ----------------

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "ChemLabGenerated");
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder(Root, "Materials");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(Root, "Prefabs");
    }

    private static void CreateMat(string fileName, string shaderName, Action<Material> init)
    {
        string path = MatFolder + "/" + fileName;
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            init(existing);
            EditorUtility.SetDirty(existing);
            return;
        }

        Shader s = Shader.Find(shaderName);
        if (s == null)
        {
            Debug.LogError("[ChemLab] Shader not found: " + shaderName + " (ensure the .shader is imported)");
            return;
        }

        var m = new Material(s);
        init(m);
        AssetDatabase.CreateAsset(m, path);
    }

    private static void CreateParticleChild(Transform parent, string name, Material mat, bool shapeCone, float rate, float startSize, float lifetime, float speed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var r = go.AddComponent<ParticleSystemRenderer>();
        r.material = mat;

        var main = ps.main;
        main.loop = true;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = startSize;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = Color.white;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        if (shapeCone)
        {
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.05f;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f;
        }

        var col = ps.colorOverLifetime;
        col.enabled = true;

        var size = ps.sizeOverLifetime;
        size.enabled = false;

        // make it not too heavy
        var limit = ps.main;
        limit.maxParticles = 512;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private struct Row
    {
        public string formula;
        public string name;
        public Color color;
        public float mp;
        public float bp;
        public int hazard;
        public int arch;
        public int preset;
        public float opacity;
        public float metal;
        public float smooth;
        public float emiss;
        public float noise;
        public float fog;
        public float bubble;
        public float visc;
        public float dens;

        public static Row Make(string f, string n, Color c, float mp, float bp, int hz, int arch, int preset,
            float opacity, float metal, float smooth, float emiss, float noise, float fog, float bubble, float visc, float dens)
        {
            return new Row
            {
                formula = f,
                name = n,
                color = c,
                mp = mp,
                bp = bp,
                hazard = hz,
                arch = arch,
                preset = preset,
                opacity = opacity,
                metal = metal,
                smooth = smooth,
                emiss = emiss,
                noise = noise,
                fog = fog,
                bubble = bubble,
                visc = visc,
                dens = dens,
            };
        }
    }

    private static Color Hex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        if (hex[0] == '#') hex = hex.Substring(1);
        if (hex.Length != 6) return Color.white;
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }
}
#endif
