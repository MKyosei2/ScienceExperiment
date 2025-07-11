#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ExperimentExecutorAutoBinder : EditorWindow
{
    [MenuItem("ChemLab/ExperimentExecutorに自動登録（displayColorなし）")]
    public static void BindExecutor()
    {
        // ExperimentExecutor を探す
        ExperimentExecutor executor = (ExperimentExecutor)FindObjectOfType(typeof(ExperimentExecutor));
        if (executor == null)
        {
            Debug.LogError("ExperimentExecutor がシーンに見つかりません。\n配置してください。");
            return;
        }

        // ElementData を検索
        string[] guids = AssetDatabase.FindAssets("t:ElementData");
        List<string> ids = new List<string>();
        List<GameObject> prefabs = new List<GameObject>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ElementData data = AssetDatabase.LoadAssetAtPath<ElementData>(path);

            if (data == null) continue;
            Debug.Log($"Found: {data.elementID}, Prefab: {(data.displayPrefab ? data.displayPrefab.name : "null")}");

            if (data.displayPrefab != null)
            {
                ids.Add(data.elementID);
                prefabs.Add(data.displayPrefab);
            }
        }

        executor.elementIDs = ids.ToArray();
        executor.elementPrefabs = prefabs.ToArray();

        // Holder 自動設定
        executor.holder = (SelectedObjectHolder)FindObjectOfType(typeof(SelectedObjectHolder));

        // SpawnPoint 自動検索（名前ベース）
        GameObject spawn = GameObject.Find("SpawnTarget");
        if (spawn != null)
            executor.spawnPoint = spawn.transform;
        else
            Debug.LogWarning("SpawnTarget が見つかりません。Transform を割り当ててください。");

        EditorUtility.SetDirty(executor);
        Debug.Log($"ExperimentExecutor に {ids.Count} 件の元素を登録しました。");
    }
}
#endif