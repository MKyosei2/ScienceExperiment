using UnityEditor;
using UnityEngine;
using UdonSharp;

public class AutoAssignZoneSpawnButtons : EditorWindow
{
    [MenuItem("ChemLab VR/🧪 RoomAssetに ZoneSpawnButton 一括設定")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssignZoneSpawnButtons>("ZoneSpawnButton 割当ツール");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("⚙️ 一括割当実行（階層内すべてに対応）"))
        {
            AssignZoneButtons();
        }
    }

    private static void AssignZoneButtons()
    {
        string[] types = { "Element", "Tool", "Condition" };
        int count = 0;

        GameObject holderObj = GameObject.Find("SelectedObjectHolder");
        var holder = holderObj ? holderObj.GetComponent<SelectedObjectHolder>() : null;

        foreach (string type in types)
        {
            string folderPath = $"RoomAsset/{type}";
            string spawnZoneName = $"{type}ExperimentZone";

            GameObject root = FindInScene(folderPath);
            GameObject spawnZone = GameObject.Find(spawnZoneName);

            if (root == null || spawnZone == null)
            {
                Debug.LogWarning($"❌ {folderPath} または {spawnZoneName} が見つかりません");
                continue;
            }

            AssignRecursively(root.transform, type, spawnZone.transform, holder, ref count);
        }

        Debug.Log($"✅ ZoneSpawnButton 一括割当完了（{count} オブジェクト）");
    }

    private static void AssignRecursively(Transform parent, string type, Transform spawnZone, SelectedObjectHolder holder, ref int count)
    {
        foreach (Transform child in parent)
        {
            GameObject go = child.gameObject;

            // ボタン化対象 = Meshあり or 名前が元素記号 or Prefabアイコンなら処理
            if (child.childCount == 0 || child.GetComponent<MeshRenderer>() || IsAtomicSymbol(go.name))
            {
                ZoneSpawnButton button = go.GetComponent<ZoneSpawnButton>();
                if (button == null)
                    button = Undo.AddComponent<ZoneSpawnButton>(go);

                button.objectType = type;
                if (button.spawnPrefab == null) button.spawnPrefab = go;
                if (button.spawnZone == null) button.spawnZone = spawnZone;
                if (button.holder == null && holder != null) button.holder = holder;

                EditorUtility.SetDirty(button);
                count++;
            }

            // 子があるなら再帰
            if (child.childCount > 0)
                AssignRecursively(child, type, spawnZone, holder, ref count);
        }
    }

    private static GameObject FindInScene(string path)
    {
        string[] names = path.Split('/');
        GameObject current = null;
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == names[0])
            {
                current = root;
                break;
            }
        }
        if (current == null) return null;

        for (int i = 1; i < names.Length; i++)
        {
            Transform child = current.transform.Find(names[i]);
            if (child == null) return null;
            current = child.gameObject;
        }

        return current;
    }

    private static bool IsAtomicSymbol(string name)
    {
        return name.Length >= 1 && name.Length <= 3 &&
               char.IsUpper(name[0]) &&
               (name.Length == 1 || char.IsLower(name[1]) || char.IsUpper(name[1]));
    }
}
