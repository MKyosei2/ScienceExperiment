using UnityEngine;
using UnityEditor;

public class AutoAssignZoneSpawnButtons : EditorWindow
{
    [MenuItem("CHEMLAB/自動登録/ZoneSpawnButton に prefab・spawnZone・objectType を設定")]
    public static void AssignButtons()
    {
        var holder = GameObject.FindObjectOfType<SelectedObjectHolder>();
        var zones = new[]
        {
            GameObject.Find("ElementExperimentZone"),
            GameObject.Find("ToolExperimentZone"),
            GameObject.Find("ConditionExperimentZone")
        };

        int count = 0;

        foreach (var button in GameObject.FindObjectsOfType<ZoneSpawnButton>())
        {
            string id = button.name;
            string lower = id.ToLower();

            // 自動判定：objectType
            if (lower.Contains("element")) button.objectType = "Element";
            else if (lower.Contains("tool")) button.objectType = "Tool";
            else if (lower.Contains("condition")) button.objectType = "Condition";

            // 自動検索：spawnPrefab（例: "Element_O" ← Button_Element_O）
            GameObject prefab = GameObject.Find(id.Replace("Button_", ""));
            if (prefab != null)
            {
                button.spawnPrefab = prefab;
            }
            else
            {
                Debug.LogWarning($"⚠️ {id.Replace("Button_", "")} が見つかりません。spawnPrefab を設定できません。");
            }

            // spawnZone の割り当て
            if (button.objectType == "Element") button.spawnZone = zones[0]?.transform;
            else if (button.objectType == "Tool") button.spawnZone = zones[1]?.transform;
            else if (button.objectType == "Condition") button.spawnZone = zones[2]?.transform;

            // holder の割り当て
            button.holder = holder;

            EditorUtility.SetDirty(button);
            count++;
        }

        Debug.Log($"✅ {count} 個の ZoneSpawnButton に prefab・spawnZone・objectType を設定しました。");
    }
}
