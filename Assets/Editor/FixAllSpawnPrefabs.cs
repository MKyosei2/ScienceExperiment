using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class FixAllSpawnPrefabs : EditorWindow
{
    private const string elementPath = "Assets/Prefab/Element";
    private const string toolPath = "Assets/Prefab/Tool";
    private const string conditionPath = "Assets/Prefab/Condition";

    [MenuItem("Tools/CHEMLAB/SpawnPrefab 自動置き換え")]
    public static void ShowWindow()
    {
        GetWindow<FixAllSpawnPrefabs>("SpawnPrefab 修復");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scene上の SpawnPrefab を Project内の正規Prefabに置き換えます。", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        if (GUILayout.Button("Prefab 一括割り当て", GUILayout.Height(40)))
        {
            FixSpawnPrefabs("Tool", toolPath);
            FixSpawnPrefabs("Element", elementPath);
            FixSpawnPrefabs("Condition", conditionPath);

            EditorUtility.DisplayDialog("完了", "全ての SpawnPrefab を修正しました。", "OK");
        }
    }

    private void FixSpawnPrefabs(string type, string prefabPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });
        var prefabDict = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p != null)
            .ToDictionary(p => p.name.ToLower(), p => p);

        ZoneSpawnButton[] allButtons = GameObject.FindObjectsOfType<ZoneSpawnButton>(true);
        int count = 0;

        foreach (var btn in allButtons)
        {
            if (btn.objectType != type) continue;
            GameObject current = btn.spawnPrefab;

            if (current == null || !AssetDatabase.Contains(current)) // not a prefab
            {
                string key = current != null ? current.name.ToLower() : "";
                if (prefabDict.TryGetValue(key, out GameObject correctPrefab))
                {
                    Undo.RecordObject(btn, "Fix Spawn Prefab");
                    btn.spawnPrefab = correctPrefab;
                    EditorUtility.SetDirty(btn);
                    count++;
                }
            }
        }

        Debug.Log($"✅ {type} 修正完了: {count} 件の SpawnPrefab を正しいプレハブに置換しました。");
    }
}
