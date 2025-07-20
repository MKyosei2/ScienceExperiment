using UnityEngine;
using UnityEditor;
using System.IO;

public class ElementMaterialGenerator : EditorWindow
{
    private string pngPath = "Assets/Texture";
    private string matSavePath = "Assets/Material";
    private float metallicValue = 0.9f;
    private float smoothnessValue = 0.6f;

    [MenuItem("Tools/元素記号マテリアル自動生成")]
    public static void ShowWindow()
    {
        GetWindow<ElementMaterialGenerator>("PNG → マテリアル生成");
    }

    private void OnGUI()
    {
        GUILayout.Label("PNG検索元フォルダ", EditorStyles.boldLabel);
        pngPath = EditorGUILayout.TextField("PNGフォルダ", pngPath);

        GUILayout.Space(10);
        GUILayout.Label("マテリアル出力フォルダ", EditorStyles.boldLabel);
        matSavePath = EditorGUILayout.TextField("保存先", matSavePath);

        GUILayout.Space(10);
        GUILayout.Label("マテリアル設定（金属用）", EditorStyles.boldLabel);
        metallicValue = EditorGUILayout.Slider("Metallic", metallicValue, 0, 1);
        smoothnessValue = EditorGUILayout.Slider("Smoothness", smoothnessValue, 0, 1);

        GUILayout.Space(20);
        if (GUILayout.Button("マテリアル一括生成"))
        {
            GenerateAllMaterials();
        }
    }

    private void GenerateAllMaterials()
    {
        if (!Directory.Exists(pngPath))
        {
            Debug.LogError("⚠️ PNGフォルダが存在しません: " + pngPath);
            return;
        }

        if (!Directory.Exists(matSavePath))
        {
            Directory.CreateDirectory(matSavePath);
        }

        string[] pngFiles = Directory.GetFiles(pngPath, "*.png");
        foreach (string filePath in pngFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string assetPath = filePath.Replace(Application.dataPath, "Assets");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture == null)
            {
                Debug.LogWarning("スキップ: テクスチャが読み込めません → " + assetPath);
                continue;
            }

            Material mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = texture;
            mat.SetFloat("_Metallic", metallicValue);
            mat.SetFloat("_Glossiness", smoothnessValue);

            string matPath = Path.Combine(matSavePath, $"{fileName}.mat");
            AssetDatabase.CreateAsset(mat, matPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 全マテリアルの生成が完了しました！");
    }
}
