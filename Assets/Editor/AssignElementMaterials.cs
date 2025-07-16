using UnityEngine;
using UnityEditor;
using System.IO;

public class AssignElementMaterials : EditorWindow
{
    private string prefabRootPath = "Assets/Element/";
    private string materialPath = "Assets/Materials/Elements/";

    [MenuItem("Tools/元素Cubeにマテリアル一括適用")]
    public static void ShowWindow()
    {
        GetWindow<AssignElementMaterials>("マテリアル適用");
    }

    private void OnGUI()
    {
        GUILayout.Label("適用設定", EditorStyles.boldLabel);
        prefabRootPath = EditorGUILayout.TextField("Prefab ルートフォルダ", prefabRootPath);
        materialPath = EditorGUILayout.TextField("マテリアルフォルダ", materialPath);

        if (GUILayout.Button("適用開始"))
        {
            ApplyMaterialsToPrefabs();
        }
    }

    private void ApplyMaterialsToPrefabs()
    {
        string[] prefabPaths = Directory.GetFiles(prefabRootPath, "*.prefab", SearchOption.AllDirectories);

        foreach (string path in prefabPaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(path); // "H", "Li" など
            string matFullPath = Path.Combine(materialPath, $"{fileName}.mat");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matFullPath);
            if (mat == null)
            {
                Debug.LogWarning($"❌ マテリアルが見つかりません: {matFullPath}");
                continue;
            }

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"❌ Prefabが読み込めません: {path}");
                continue;
            }

            // 一時インスタンス生成（Prefabを編集不可なままでは変更できない）
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
            if (instance == null) continue;

            MeshRenderer renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"❌ Rendererが見つかりません: {fileName}");
                DestroyImmediate(instance);
                continue;
            }

            // 同じマテリアルを6面分に割り当て
            Material[] mats = new Material[6];
            for (int i = 0; i < 6; i++) mats[i] = mat;
            renderer.sharedMaterials = mats;

            // Prefabに反映
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);

            Debug.Log($"✅ 適用: {fileName} → {mat.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("🎉 全てのPrefabにマテリアルを適用しました！");
    }
}