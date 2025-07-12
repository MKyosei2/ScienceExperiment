using UnityEngine;
using UnityEditor;

public class SelectorObjectAutoSetter : EditorWindow
{
    private GameObject[] targets;
    private string objectType = "Element";

    [MenuItem("CHEMLAB/Selector Object Auto Setter")]
    public static void ShowWindow()
    {
        GetWindow<SelectorObjectAutoSetter>("SelectorObject Setter");
    }

    private void OnGUI()
    {
        GUILayout.Label("一括SelectorObject設定", EditorStyles.boldLabel);

        objectType = EditorGUILayout.TextField("Object Type", objectType);

        if (GUILayout.Button("選択中のオブジェクトに自動設定する"))
        {
            SetSelectorObjects();
        }
    }

    private void SetSelectorObjects()
    {
        targets = Selection.gameObjects;

        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("何も選択されていません。オブジェクトを選択してください。");
            return;
        }

        foreach (GameObject obj in targets)
        {
            Undo.RegisterCompleteObjectUndo(obj, "Add SelectorObject");

            // SelectorObjectが無ければ追加
            SelectorObject selector = obj.GetComponent<SelectorObject>();
            if (selector == null)
            {
                selector = obj.AddComponent<SelectorObject>();
            }

            selector.objectType = objectType;
            selector.objectID = obj.name;

            EditorUtility.SetDirty(obj);
            Debug.Log($"{obj.name} に SelectorObject を設定しました: Type={objectType}, ID={obj.name}");
        }
    }
}
