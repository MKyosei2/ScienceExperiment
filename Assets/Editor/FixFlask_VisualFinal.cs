using UnityEngine;
using UnityEditor;

public class FixFlask_VisualFinal : EditorWindow
{
    private const string PREFAB_PATH = "Assets/Prefabs/CONICAL_FLASK.prefab";

    [MenuItem("Tools/Fix/Fix Flask Visuals (Final)")]
    public static void Fix()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);

        if (prefab == null)
        {
            Debug.LogError("❌ Prefab not found: " + PREFAB_PATH);
            return;
        }

        GameObject inst = Instantiate(prefab);
        inst.name = "CONICAL_FLASK_VisualFixTemp";

        Debug.Log("🔧 Starting FINAL visual fix…");

        // -----------------------------------------
        // STEP 1: fix MeshRenderer / WireframeFX
        // -----------------------------------------

        Transform modelT = inst.transform.Find("Model");

        if (modelT != null)
        {
            MeshRenderer wireMR = modelT.GetComponent<MeshRenderer>();
            if (wireMR != null)
            {
                Material wireMat = wireMR.sharedMaterial;
                if (wireMat != null)
                {
                    wireMat.renderQueue = 2000;  // Geometry
                    Debug.Log("✔ WireframeFX Material renderQueue set to 2000");
                }
            }
        }

        // -----------------------------------------
        // STEP 2: fix Liquid Particle
        // -----------------------------------------

        Transform liquidParticleT = inst.transform.Find("Particle");
        if (liquidParticleT == null)
        {
            Debug.LogError("❌ No Particle object found in prefab.");
            DestroyImmediate(inst);
            return;
        }

        ParticleSystem ps = liquidParticleT.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError("❌ Particle exists but has no ParticleSystem!");
            DestroyImmediate(inst);
            return;
        }

        // Enable emission
        var emission = ps.emission;
        emission.rateOverTime = 10f;

        // -----------------------------------------
        // STEP 3: Fix Particle Material
        // -----------------------------------------

        Material liquidMat = new Material(Shader.Find("Particles/Standard Unlit"));
        liquidMat.name = "LiquidParticleMaterial_Final";

        // Shader states (depth + transparency)
        liquidMat.SetInt("_ZWrite", 0);        // Do NOT write to depth
        liquidMat.renderQueue = 3000;          // Transparent queue
        liquidMat.SetColor("_Color", Color.white);

        // -----------------------------------------
        // STEP 4: Renderer settings
        // -----------------------------------------

        ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
        pr.material = liquidMat;
        pr.renderMode = ParticleSystemRenderMode.Billboard;

        // Draw on top of WireframeFX
        pr.sortingOrder = 200;

        Debug.Log("✔ Particle material & sorting fully fixed.");

        // -----------------------------------------
        // STEP 5: Save as prefab
        // -----------------------------------------

        PrefabUtility.SaveAsPrefabAsset(inst, PREFAB_PATH);
        DestroyImmediate(inst);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 Visual Fix Complete! Particles will now render fully & always visible.");
    }
}
