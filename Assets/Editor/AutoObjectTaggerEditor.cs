using UnityEngine;
using UnityEditor;

public class AutoObjectTaggerEditor : EditorWindow
{
    private string[] objectTypes = new string[] { "Element", "Tool", "Condition" };
    private GameObject[] targets;
    private int selectedTypeIndex = 0;

    [MenuItem("ChemLab/Auto Tag Selector Objects")]
    public static void ShowWindow()
    {
        GetWindow<AutoObjectTaggerEditor>("Auto Tag Selector Objects");
    }

    void OnGUI()
    {
        GUILayout.Label("自動タグ付けツール", EditorStyles.boldLabel);

        selectedTypeIndex = EditorGUILayout.Popup("Object Type", selectedTypeIndex, objectTypes);

        EditorGUILayout.Space();
        if (GUILayout.Button("現在の選択を取得"))
        {
            targets = Selection.gameObjects;
        }

        if (targets != null && targets.Length > 0)
        {
            EditorGUILayout.LabelField($"選択対象: {targets.Length} 個");

            if (GUILayout.Button("SelectorObject を自動設定"))
            {
                int updated = 0;

                foreach (var go in targets)
                {
                    var selector = go.GetComponent<SelectorObject>();
                    if (selector == null)
                    {
                        selector = Undo.AddComponent<SelectorObject>(go);
                    }

                    if (selector != null)
                    {
                        SerializedObject soSelector = new SerializedObject(selector);
                        SerializedProperty propType = soSelector.FindProperty("objectType");
                        SerializedProperty propID = soSelector.FindProperty("objectID");

                        if (propType != null && propType.propertyType == SerializedPropertyType.String)
                        {
                            propType.stringValue = objectTypes[selectedTypeIndex];
                        }
                        if (propID != null && propID.propertyType == SerializedPropertyType.String)
                        {
                            propID.stringValue = go.name;
                        }

                        soSelector.ApplyModifiedProperties();
                        EditorUtility.SetDirty(selector);
                        updated++;
                    }
                }

                Debug.Log($"{updated} 個の SelectorObject にデータを適用しました。");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("GameObject を選択してください。", MessageType.Info);
        }
    }
}
