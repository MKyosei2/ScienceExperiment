// Assets/Editor/AutoAssignSelectionZoneObjects.cs

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AutoAssignSelectionZoneObjects : EditorWindow
{
    [MenuItem("CHEMLAB/自動登録/SelectionZoneにElementオブジェクトを登録")]
    public static void AssignToSelectionZone()
    {
        var zone = FindObjectOfType<SelectionZone>();
        if (zone == null)
        {
            Debug.LogError("SelectionZone が見つかりません。シーンに1つ以上置いてください。");
            return;
        }

        GameObject elementRoot = GameObject.Find("Element");
        if (elementRoot == null)
        {
            Debug.LogError("Hierarchy に 'Element' オブジェクトが見つかりません。");
            return;
        }

        List<GameObject> elements = new List<GameObject>();
        foreach (Transform group in elementRoot.transform)
        {
            foreach (Transform element in group)
            {
                elements.Add(element.gameObject);
            }
        }

        zone.objectsInZone = elements.ToArray();
        EditorUtility.SetDirty(zone);
        Debug.Log($"✅ SelectionZone に {elements.Count} 個の Element を登録しました。");
    }
}
