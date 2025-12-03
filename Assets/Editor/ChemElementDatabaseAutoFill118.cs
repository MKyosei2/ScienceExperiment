using UnityEngine;
using UnityEditor;

public class ChemElementDatabaseAutoFill118 : EditorWindow
{
    private ChemElementDatabase db;

    [MenuItem("ChemLab/AutoFill 118 Elements")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ChemElementDatabaseAutoFill118));
    }

    private void OnGUI()
    {
        GUILayout.Label("ChemElementDatabase 118元素 自動登録", EditorStyles.boldLabel);

        db = (ChemElementDatabase)EditorGUILayout.ObjectField(
            "Database", db, typeof(ChemElementDatabase), true);

        if (db == null) return;

        if (GUILayout.Button("Fill Database with Available Data (Safe Mode)"))
        {
            Fill();
        }
    }

    private void Fill()
    {
        Undo.RecordObject(db, "AutoFill 118 Elements");

        // === 必ず 118 個で作成 ===
        db.elements = new ElementData[118];
        for (int i = 0; i < 118; i++)
            db.elements[i] = new ElementData();


        // === 入れるデータ（例：10〜20個のみ）===
        string[] names = new string[]
        {
            "H","He","Li","Be","B","C","N","O","F","Ne"
        };

        string[] groups = new string[]
        {
            "nonmetal","noble","metal","metal","metalloid",
            "nonmetal","nonmetal","nonmetal","halogen","noble"
        };

        Color[] colors = new Color[]
        {
            Color.white,
            new Color(0.8f,0.9f,1f),
            Color.red,
            Color.green,
            Color.cyan,
            Color.black,
            Color.blue,
            Color.red,
            Color.green,
            new Color(0.8f,0.8f,1f)
        };

        float[] melt = new float[]
        {
            14f,1f,453f,1560f,2349f,3800f,63f,54f,53f,25f
        };

        float[] boil = new float[]
        {
            20f,4f,1603f,2742f,4200f,4300f,77f,90f,85f,27f
        };


        // ======================================
        //      ★ 安全代入（最小データ数のみ）
        // ======================================
        int count = Mathf.Min(
            names.Length,
            groups.Length,
            colors.Length,
            melt.Length,
            boil.Length
        );

        for (int i = 0; i < count; i++)
        {
            db.elements[i].symbol = names[i];
            db.elements[i].group = groups[i];
            db.elements[i].color = colors[i];
            db.elements[i].meltingPoint = melt[i];
            db.elements[i].boilingPoint = boil[i];
        }

        // 残りは空欄のまま
        EditorUtility.SetDirty(db);

        Debug.Log("✔ 118個分の枠を作成し、利用可能なデータだけ安全に登録しました！");
    }
}
