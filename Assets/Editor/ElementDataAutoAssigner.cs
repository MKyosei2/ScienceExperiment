using UnityEngine;
using UnityEditor;

public class ElementDataAutoAssigner : EditorWindow
{
    [MenuItem("Tools/Auto Assign Element Prefabs")]
    public static void AssignPrefabsToElementData()
    {
        string prefabFolderPath = "Assets/Prefab/Element/";

        string[] guids = AssetDatabase.FindAssets("t:ElementData");
        int assignedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ElementData elementData = AssetDatabase.LoadAssetAtPath<ElementData>(assetPath);

            if (elementData == null || string.IsNullOrEmpty(elementData.elementID))
                continue;

            string prefabPath = prefabFolderPath + elementData.elementID + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                Undo.RecordObject(elementData, "Assign Display Prefab");
                elementData.displayPrefab = prefab;
                EditorUtility.SetDirty(elementData);
                assignedCount++;
            }
            else
            {
                Debug.LogWarning($"⚠️ Prefab not found for elementID '{elementData.elementID}' at path: {prefabPath}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Auto assignment complete. {assignedCount} prefabs assigned.");
    }
}
