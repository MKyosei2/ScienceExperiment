// AutoFillSharedFlask.cs
// flaskPrefabs[] を全要素 SharedFlaskPrefab に自動で埋める

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AutoFillSharedFlask
{
    [MenuItem("VRC Lab/Auto Fill SharedFlaskPrefab")]
    public static void Fill()
    {
        var manager = GameObject.FindObjectOfType<ChemEnvironmentManager>();
        if (manager == null)
        {
            Debug.LogError("[AutoFillSharedFlask] ChemEnvironmentManager が Hierarchy に見つかりません。");
            return;
        }

        // 手動で選択した Prefab を使う
        string prefabPath = EditorUtility.OpenFilePanel("Select SharedFlaskPrefab", "Assets", "prefab");
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("[AutoFillSharedFlask] Prefab が選ばれませんでした。");
            return;
        }

        // プロジェクトパスを相対パスに変換
        string projectPath = Application.dataPath;
        if (prefabPath.StartsWith(projectPath))
        {
            prefabPath = "Assets" + prefabPath.Substring(projectPath.Length);
        }

        GameObject shared = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (shared == null)
        {
            Debug.LogError("[AutoFillSharedFlask] Prefab のロードに失敗しました: " + prefabPath);
            return;
        }

        // 配列すべてに同じPrefabを割り当て
        for (int i = 0; i < manager.flaskPrefabs.Length; i++)
        {
            manager.flaskPrefabs[i] = shared;
        }

        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();

        Debug.Log("[AutoFillSharedFlask] flaskPrefabs の全 " + manager.flaskPrefabs.Length + " 要素に "
                  + shared.name + " を割り当てました。");
    }
}
#endif

