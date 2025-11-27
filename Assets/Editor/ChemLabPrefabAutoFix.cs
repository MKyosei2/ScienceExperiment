using UnityEngine;
using UnityEditor;

public class ChemLabPrefabAutoFix : EditorWindow
{
    [MenuItem("ChemLab/AutoFix CONICAL_FLASK Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ChemLabPrefabAutoFix>("ChemLab Prefab AutoFix");
    }

    private void OnGUI()
    {
        GUILayout.Label("CONICAL_FLASK Prefab 修復ツール", EditorStyles.boldLabel);

        if (GUILayout.Button("Prefab を一括修正する"))
        {
            FixPrefab();
        }
    }

    private static void FixPrefab()
    {
        GameObject prefab = Selection.activeObject as GameObject;
        if (prefab == null)
        {
            Debug.LogError("❌ Prefab（CONICAL_FLASK）を Project で選択してから実行してください");
            return;
        }

        string path = AssetDatabase.GetAssetPath(prefab);
        if (!path.EndsWith(".prefab"))
        {
            Debug.LogError("❌ 選択されたオブジェクトは Prefab ではありません");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(path);

        // ======================================================
        // Model の確認
        // ======================================================
        Transform model = root.transform.Find("Model");
        if (model == null)
        {
            Debug.LogError("❌ Model がありません");
        }

        // ======================================================
        // LiquidContainer の確認
        // ======================================================
        Transform container = root.transform.Find("LiquidContainer");
        if (container == null)
        {
            container = new GameObject("LiquidContainer").transform;
            container.SetParent(root.transform);
            container.localPosition = Vector3.zero;
        }

        // ======================================================
        // LiquidSurface の生成 or 修正
        // ======================================================
        Transform ls = container.Find("LiquidSurface");
        if (ls == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "LiquidSurface";
            ls = go.transform;
            ls.SetParent(container);
        }

        // Transform 設定
        ls.localPosition = new Vector3(0f, 0.045f, 0f);
        ls.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ls.localScale = new Vector3(0.015f, 0.015f, 0.015f);

        // Material 設定
        var smr = ls.GetComponent<MeshRenderer>();
        Material surfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LiquidSurface.mat");
        if (surfMat != null)
        {
            smr.sharedMaterial = surfMat;
        }

        // MeshFilter 設定
        var mf = ls.GetComponent<MeshFilter>();
        mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        // ======================================================
        // LiquidParticle 修正
        // ======================================================
        Transform lp = container.Find("LiquidParticle");
        if (lp == null)
        {
            lp = new GameObject("LiquidParticle").transform;
            lp.SetParent(container);
            lp.localPosition = Vector3.zero;
            lp.gameObject.AddComponent<ParticleSystem>();
        }

        // ======================================================
        // 保存
        // ======================================================
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("✔ CONICAL_FLASK Prefab の修復が完了しました！");
    }
}
