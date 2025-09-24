#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class PrefabGenerator
{
    [MenuItem("Tools/Generate Chem Prefabs")]
    public static void GeneratePrefabs()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // === LabelPrefab ===
        GameObject label = new GameObject("ElementLabel");
        var tmp = label.AddComponent<TextMeshPro>();
        tmp.text = "H"; // 仮
        tmp.fontSize = 1f;
        tmp.alignment = TextAlignmentOptions.Center;
        PrefabUtility.SaveAsPrefabAsset(label, folder + "/LabelPrefab.prefab");
        Object.DestroyImmediate(label);

        // === SingleBondPrefab ===
        GameObject singleBond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        singleBond.name = "SingleBond";
        singleBond.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
        PrefabUtility.SaveAsPrefabAsset(singleBond, folder + "/SingleBondPrefab.prefab");
        Object.DestroyImmediate(singleBond);

        // === DoubleBondPrefab ===
        GameObject doubleBond = new GameObject("DoubleBond");
        GameObject db1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        db1.transform.SetParent(doubleBond.transform, false);
        db1.transform.localPosition = new Vector3(0.1f, 0, 0);
        db1.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);

        GameObject db2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        db2.transform.SetParent(doubleBond.transform, false);
        db2.transform.localPosition = new Vector3(-0.1f, 0, 0);
        db2.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);

        PrefabUtility.SaveAsPrefabAsset(doubleBond, folder + "/DoubleBondPrefab.prefab");
        Object.DestroyImmediate(doubleBond);

        // === TripleBondPrefab ===
        GameObject tripleBond = new GameObject("TripleBond");

        GameObject tb1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tb1.transform.SetParent(tripleBond.transform, false);
        tb1.transform.localPosition = new Vector3(0.15f, 0, 0);
        tb1.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);

        GameObject tb2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tb2.transform.SetParent(tripleBond.transform, false);
        tb2.transform.localPosition = new Vector3(0f, 0, 0);
        tb2.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);

        GameObject tb3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tb3.transform.SetParent(tripleBond.transform, false);
        tb3.transform.localPosition = new Vector3(-0.15f, 0, 0);
        tb3.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);

        PrefabUtility.SaveAsPrefabAsset(tripleBond, folder + "/TripleBondPrefab.prefab");
        Object.DestroyImmediate(tripleBond);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ Prefabs generated at: " + folder);
    }
}
#endif
