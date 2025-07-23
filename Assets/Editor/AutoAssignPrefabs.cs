using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class AutoAssignPrefabs : EditorWindow
{
    private const string toolPath = "Assets/Prefab/Tool";
    private const string elementPath = "Assets/Prefab/Element";
    private const string conditionPath = "Assets/Prefab/Condition";

    [MenuItem("Tools/CHEMLAB/Prefabを自動アサイン")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssignPrefabs>("Prefab 自動アサイン");
    }

    private void OnGUI()
    {
        GUILayout.Label("Selector に正しい Prefab をアサインします", EditorStyles.boldLabel);
        if (GUILayout.Button("自動アサイン 実行", GUILayout.Height(40)))
        {
            AssignPrefabs<ToolSelector>(toolPath, "toolPrefab");
            AssignPrefabs<ElementSelector>(elementPath, "elementPrefab");
            AssignPrefabs<ConditionSelector>(conditionPath, "conditionPrefab");

            EditorUtility.DisplayDialog("完了", "すべてのSelectorにPrefabをアサインしました。", "OK");
        }
    }

    private void AssignPrefabs<T>(string folderPath, string fieldName) where T : MonoBehaviour
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        GameObject[] prefabs = guids.Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToArray();

        T[] selectors = GameObject.FindObjectsOfType<T>(true);

        foreach (var selector in selectors)
        {
            SerializedObject so = new SerializedObject(selector);
            SerializedProperty prop = so.FindProperty(fieldName);

            GameObject match = prefabs.FirstOrDefault(p => p.name.ToLower() == selector.gameObject.name.ToLower());
            if (match != null)
            {
                prop.objectReferenceValue = match;
                so.ApplyModifiedProperties();
                Debug.Log($"? {selector.name} に {match.name} をアサイン");
            }
            else
            {
                Debug.LogWarning($"?? {selector.name} に対応するプレハブが {folderPath} に見つかりません");
            }
        }
    }
}
