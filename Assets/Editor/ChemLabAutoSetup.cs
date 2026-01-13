using UnityEditor;
using UnityEngine;
using System.Linq;

public static class ChemLabAutoSetup
{
    [MenuItem("VRC ChemLab/Auto Setup Runtime Tool Prefabs")]
    public static void AutoSetup()
    {
        var spawners = Object.FindObjectsOfType<ChemRuntimeToolSpawner>(true);
        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("ChemRuntimeToolSpawner not found in scene.");
            return;
        }

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        var prefabs = prefabGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
            .Where(p => p != null)
            .OrderBy(p => p.name)
            .ToArray();

        foreach (var s in spawners)
        {
            Undo.RecordObject(s, "Auto Setup Runtime Tool Prefabs");
            s.enablePrefabSpawn = true;
            s.toolPrefabs = prefabs;
            s.toolIds = prefabs.Select(p => p.name).ToArray();
            EditorUtility.SetDirty(s);
        }

        Debug.Log($"AutoSetup complete. Prefabs: {prefabs.Length}, Spawners: {spawners.Length}");
    }
}
