using UnityEditor;
using UnityEngine;

public class DeleteAllSpawnPoints : EditorWindow
{
    [MenuItem("Tools/CHEMLAB/削除: SpawnPoint をすべて消す")]
    public static void ShowWindow()
    {
        GetWindow<DeleteAllSpawnPoints>("SpawnPoint 一括削除");
    }

    private void OnGUI()
    {
        GUILayout.Label("Hierarchy 内のすべての 'SpawnPoint' を削除します。", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("SpawnPoint を全削除", GUILayout.Height(40)))
        {
            DeleteSpawnPoints();
        }
    }

    private void DeleteSpawnPoints()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "SpawnPoint")
            {
                Undo.DestroyObjectImmediate(obj);
                count++;
            }
        }

        Debug.Log($"✅ SpawnPoint 削除完了: {count} 件");
        EditorUtility.DisplayDialog("完了", $"SpawnPoint を {count} 件削除しました。", "OK");
    }
}
