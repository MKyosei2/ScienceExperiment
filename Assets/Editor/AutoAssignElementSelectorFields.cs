// Assets/Editor/AutoAssignElementSelectorFields.cs

using UnityEngine;
using UnityEditor;

public class AutoAssignElementSelectorFields : EditorWindow
{
    [MenuItem("CHEMLAB/自動登録/ElementSelector に Prefab と SpawnPoint（自動生成）を設定")]
    public static void AssignElementSelectors()
    {
        GameObject elementRoot = GameObject.Find("Element");
        if (elementRoot == null)
        {
            Debug.LogError("Hierarchy に 'Element' オブジェクトが見つかりません。");
            return;
        }

        int assignedCount = 0;

        foreach (Transform group in elementRoot.transform)
        {
            foreach (Transform element in group)
            {
                var selector = element.GetComponent<ElementSelector>();
                if (selector == null) continue;

                selector.elementPrefab = element.gameObject;

                // 子に SpawnPoint がなければ新しく作る
                Transform spawn = element.Find("SpawnPoint");
                if (spawn == null)
                {
                    GameObject spawnObj = new GameObject("SpawnPoint");
                    spawnObj.transform.SetParent(element, false);
                    spawnObj.transform.localPosition = new Vector3(0, 0.5f, 0); // 任意の位置
                    spawn = spawnObj.transform;
                }

                selector.spawnPoint = spawn;

                EditorUtility.SetDirty(selector);
                assignedCount++;
            }
        }

        Debug.Log($"✅ {assignedCount} 個の ElementSelector に Prefab と SpawnPoint を設定しました。");
    }
}
