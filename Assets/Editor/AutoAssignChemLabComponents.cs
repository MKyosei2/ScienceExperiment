using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;

public class AutoAssignChemLabComponents : EditorWindow
{
    [MenuItem("ChemLab VR/一括スクリプト割り当て")]
    public static void ShowWindow()
    {
        GetWindow<AutoAssignChemLabComponents>("ChemLab 自動割り当て");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("⚙️ RoomAsset内のPrefabにスクリプト自動追加"))
        {
            AssignComponentsInRoomAssets();
        }
    }

    private static void AssignComponentsInRoomAssets()
    {
        string[] targets = { "RoomAsset/Element", "RoomAsset/Tool", "RoomAsset/Condition" };

        foreach (string path in targets)
        {
            GameObject root = GameObject.Find(path);
            if (root == null)
            {
                Debug.LogWarning($"❌ {path} がHierarchyに存在しません");
                continue;
            }

            foreach (Transform child in root.transform)
            {
                if (child == null) continue;
                AssignToSingleObject(child.gameObject, path);
            }
        }

        Debug.Log("✅ 自動割り当て完了！");
    }

    private static void AssignToSingleObject(GameObject obj, string categoryPath)
    {
        if (obj.GetComponent<SelectorObject>() == null)
            obj.AddComponent<SelectorObject>().SetObjectType(GetTypeFromPath(categoryPath));

        if (obj.GetComponent<ZoneAwareObject>() == null)
            obj.AddComponent<ZoneAwareObject>();

        // Element/Tool/Condition に応じて個別に処理
        if (categoryPath.Contains("Element"))
        {
            if (obj.GetComponent<ElementSelector>() == null)
                obj.AddComponent<ElementSelector>();
        }
        else if (categoryPath.Contains("Tool"))
        {
            if (obj.GetComponent<ToolSelector>() == null)
                obj.AddComponent<ToolSelector>();
        }
        else if (categoryPath.Contains("Condition"))
        {
            if (obj.GetComponent<ConditionSelector>() == null)
                obj.AddComponent<ConditionSelector>();
        }

        // RigidbodyとColliderがなければ追加（VR Pickupのため）
        if (obj.GetComponent<Rigidbody>() == null)
            obj.AddComponent<Rigidbody>().useGravity = false;

        if (obj.GetComponent<Collider>() == null)
            obj.AddComponent<BoxCollider>();
    }

    private static string GetTypeFromPath(string path)
    {
        if (path.Contains("Element")) return "Element";
        if (path.Contains("Tool")) return "Tool";
        if (path.Contains("Condition")) return "Condition";
        return "Unknown";
    }
}
