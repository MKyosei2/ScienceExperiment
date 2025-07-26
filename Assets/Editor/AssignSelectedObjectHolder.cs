using UnityEditor;
using UnityEngine;
using UdonSharp;

public class AssignSelectedObjectHolder : EditorWindow
{
    private SelectedObjectHolder holder;

    [MenuItem("Tools/CHEMLAB/一括 Holder 割り当て")]
    public static void ShowWindow()
    {
        GetWindow<AssignSelectedObjectHolder>("Holder 割り当てツール");
    }

    private void OnGUI()
    {
        GUILayout.Label("CHEMLAB VR 用 Holder 一括割り当て", EditorStyles.boldLabel);

        holder = (SelectedObjectHolder)EditorGUILayout.ObjectField("割り当てる Holder", holder, typeof(SelectedObjectHolder), true);

        if (GUILayout.Button("すべてのオブジェクトに一括設定"))
        {
            if (holder == null)
            {
                EditorUtility.DisplayDialog("エラー", "SelectedObjectHolder が設定されていません。", "OK");
                return;
            }

            AssignToAll(holder);
        }
    }

    private void AssignToAll(SelectedObjectHolder targetHolder)
    {
        int count = 0;

        // Scene 内のすべての GameObject を探索
        foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
        {
            if (!go.scene.IsValid()) continue; // シーン上のオブジェクトのみ

            // SelectorObject
            var selector = go.GetComponent<SelectorObject>();
            if (selector != null)
            {
                Undo.RecordObject(selector, "Assign Holder to SelectorObject");
                selector.holder = targetHolder;
                EditorUtility.SetDirty(selector);
                count++;
            }

            // ZoneSpawnButton
            var spawnBtn = go.GetComponent<ZoneSpawnButton>();
            if (spawnBtn != null)
            {
                Undo.RecordObject(spawnBtn, "Assign Holder to ZoneSpawnButton");
                spawnBtn.holder = targetHolder;
                EditorUtility.SetDirty(spawnBtn);
                count++;
            }

            // ZoneSelectionButton
            var selectBtn = go.GetComponent<ZoneSelectionButton>();
            if (selectBtn != null)
            {
                Undo.RecordObject(selectBtn, "Assign Holder to ZoneSelectionButton");
                selectBtn.holder = targetHolder;
                EditorUtility.SetDirty(selectBtn);
                count++;
            }
        }

        EditorUtility.DisplayDialog("完了", $"合計 {count} 個のオブジェクトに holder を設定しました。", "OK");
    }
}