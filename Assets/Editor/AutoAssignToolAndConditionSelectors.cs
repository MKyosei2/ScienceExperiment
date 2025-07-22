// Assets/Editor/AutoAssignToolAndConditionSelectors.cs

using UnityEngine;
using UnityEditor;

public class AutoAssignToolAndConditionSelectors : EditorWindow
{
    [MenuItem("CHEMLAB/自動登録/Tool & Condition Selector に Prefab と SpawnPoint を設定")]
    public static void AssignSelectors()
    {
        AssignToolSelectors();
        AssignConditionSelectors();
    }

    private static void AssignToolSelectors()
    {
        GameObject toolRoot = GameObject.Find("Tool");
        if (toolRoot == null)
        {
            Debug.LogWarning("Hierarchy に 'Tool' が見つかりません。");
            return;
        }

        int count = 0;
        foreach (Transform tool in toolRoot.transform)
        {
            var selector = tool.GetComponent<ToolSelector>();
            if (selector == null) continue;

            selector.toolPrefab = tool.gameObject;

            Transform spawn = tool.Find("SpawnPoint");
            if (spawn == null)
            {
                GameObject spawnObj = new GameObject("SpawnPoint");
                spawnObj.transform.SetParent(tool, false);
                spawnObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                spawn = spawnObj.transform;
            }

            selector.spawnPoint = spawn;
            EditorUtility.SetDirty(selector);
            count++;
        }

        Debug.Log($"✅ {count} 個の ToolSelector に Prefab と SpawnPoint を設定しました。");
    }

    private static void AssignConditionSelectors()
    {
        GameObject conditionRoot = GameObject.Find("Condition");
        if (conditionRoot == null)
        {
            Debug.LogWarning("Hierarchy に 'Condition' が見つかりません。");
            return;
        }

        int count = 0;
        foreach (Transform condition in conditionRoot.transform)
        {
            var selector = condition.GetComponent<ConditionSelector>();
            if (selector == null) continue;

            selector.conditionPrefab = condition.gameObject;

            Transform spawn = condition.Find("SpawnPoint");
            if (spawn == null)
            {
                GameObject spawnObj = new GameObject("SpawnPoint");
                spawnObj.transform.SetParent(condition, false);
                spawnObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                spawn = spawnObj.transform;
            }

            selector.spawnPoint = spawn;
            EditorUtility.SetDirty(selector);
            count++;
        }

        Debug.Log($"✅ {count} 個の ConditionSelector に Prefab と SpawnPoint を設定しました。");
    }
}
