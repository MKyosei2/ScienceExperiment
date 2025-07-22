// Assets/Editor/AutoAssignZoneSelectionSpawner.cs

using UnityEngine;
using UnityEditor;

public class AutoAssignZoneSelectionSpawner : EditorWindow
{
    [MenuItem("CHEMLAB/自動登録/ZoneSelectionSpawner の項目を自動設定")]
    public static void AssignZoneSpawners()
    {
        string[] zoneNames = { "ElementExperimentZone", "ToolExperimentZone", "ConditionExperimentZone" };

        int assigned = 0;

        foreach (string zoneName in zoneNames)
        {
            GameObject zoneObj = GameObject.Find(zoneName);
            if (zoneObj == null)
            {
                Debug.LogWarning($"⚠️ {zoneName} が Hierarchy に見つかりません。スキップします。");
                continue;
            }

            var spawner = zoneObj.GetComponent<ZoneSelectionSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning($"⚠️ {zoneName} に ZoneSelectionSpawner がありません。スキップします。");
                continue;
            }

            // 1. ZoneType を設定（名前から推測）
            if (zoneName.Contains("Element")) spawner.zoneType = "Element";
            else if (zoneName.Contains("Tool")) spawner.zoneType = "Tool";
            else if (zoneName.Contains("Condition")) spawner.zoneType = "Condition";

            // 2. Spawn Target を設定（子に "SpawnTarget" があれば、それ。なければ自分自身）
            Transform spawnPoint = zoneObj.transform.Find("SpawnTarget");
            spawner.spawnTarget = spawnPoint != null ? spawnPoint : zoneObj.transform;

            // Prefabは任意なのでスキップ（nullのままでOK）

            EditorUtility.SetDirty(spawner);
            assigned++;
        }

        Debug.Log($"✅ {assigned} 個の ZoneSelectionSpawner に zoneType と spawnTarget を設定しました。");
    }
}