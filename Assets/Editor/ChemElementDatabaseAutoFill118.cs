using UnityEngine;
using UnityEditor;

public class ChemElementDatabaseAutoFill118 : EditorWindow
{
    private ChemElementDatabase db;

    [MenuItem("ChemLab/Fill Database (118 Elements)")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ChemElementDatabaseAutoFill118), false, "DB AutoFill 118");
    }

    private void OnGUI()
    {
        GUILayout.Label("118元素データ自動入力ツール", EditorStyles.boldLabel);

        db = EditorGUILayout.ObjectField("Database", db, typeof(ChemElementDatabase), true) as ChemElementDatabase;

        if (GUILayout.Button("▶ 118元素データを自動入力する（周期表順）"))
        {
            if (db == null)
            {
                Debug.LogError("ChemElementDatabase を指定してください");
                return;
            }

            Fill118(db);
        }
    }

    // ============================================================
    // 118元素データ自動入力
    // ============================================================
    private void Fill118(ChemElementDatabase db)
    {
        // ───────────────────────────────────────────
        // 周期表 1〜118 の Symbol
        // ───────────────────────────────────────────
        string[] symbols = {
            "H","He","Li","Be","B","C","N","O","F","Ne",
            "Na","Mg","Al","Si","P","S","Cl","Ar","K","Ca",
            "Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
            "Ga","Ge","As","Se","Br","Kr","Rb","Sr","Y","Zr",
            "Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd","In","Sn",
            "Sb","Te","I","Xe","Cs","Ba","La","Ce","Pr","Nd",
            "Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er","Tm","Yb",
            "Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg",
            "Tl","Pb","Bi","Po","At","Rn","Fr","Ra","Ac","Th",
            "Pa","U","Np","Pu","Am","Cm","Bk","Cf","Es","Fm",
            "Md","No","Lr","Rf","Db","Sg","Bh","Hs","Mt","Ds",
            "Rg","Cn","Nh","Fl","Mc","Lv","Ts","Og"
        };

        // ───────────────────────────────────────────
        // 分類（方式2）
        // ───────────────────────────────────────────
        string[] groups = {
            "nonmetal","noble","metal","metal","metalloid","nonmetal","nonmetal","nonmetal","halogen","noble",
            "metal","metal","metal","metalloid","nonmetal","nonmetal","halogen","noble","metal","metal",
            "metal","metal","metal","metal","metal","metal","metal","metal","metal","metal",
            "metal","metalloid","metalloid","nonmetal","halogen","noble","metal","metal","metal","metal",
            "metal","metal","metal","metal","metal","metal","metal","metal","metal","metal",
            "metalloid","metalloid","halogen","noble","metal","metal","lanact","lanact","lanact","lanact",
            "lanact","lanact","lanact","lanact","lanact","lanact","lanact","lanact","lanact","lanact",
            "lanact","metal","metal","metal","metal","metal","metal","metal","metal","metal",
            "metal","metal","metal","metal","halogen","noble","metal","metal","lanact","lanact",
            "lanact","lanact","lanact","lanact","lanact","lanact","lanact","lanact","lanact","lanact",
            "lanact","lanact","lanact","metal","metal","metal","metal","metal","metal","metal",
            "metal","metal","metal","metal","metal","metal","halogen","noble"
        };

        // ───────────────────────────────────────────
        // 固体（液体）代表色
        // （気体でも固体色で見えるように）
        // ───────────────────────────────────────────
        Color[] colors = new Color[symbols.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            string g = groups[i];

            if (g == "metal")
                colors[i] = new Color(0.75f, 0.75f, 0.8f); // 金属光沢の明灰色
            else if (g == "metalloid")
                colors[i] = new Color(0.5f, 0.5f, 0.6f);   // 半金属：少し暗め
            else if (g == "nonmetal")
                colors[i] = new Color(0.9f, 0.9f, 0.9f);   // 白系（固体非金属）
            else if (g == "halogen")
                colors[i] = new Color(0.9f, 0.9f, 0.3f);   // 黄緑系（ハロゲン代表）
            else if (g == "noble")
                colors[i] = new Color(0.8f, 0.8f, 1.0f);   // 淡青〜淡紫（固体貴ガス）
            else if (g == "lanact")
                colors[i] = new Color(0.6f, 0.7f, 0.6f);   // ランタノイド/アクチノイド：緑灰系
            else
                colors[i] = Color.white;
        }

        // ───────────────────────────────────────────
        // 代表的な融点・沸点（必要性に応じ簡易化）
        // Udon の負荷の都合もあり、ここは大まかな範囲で登録
        // ───────────────────────────────────────────

        // ★ 注意：ここでは簡略化した融点/沸点テーブルを挿入します
        //   → 必要なら後であなたの用途に合わせて実数値版を作成可能

        float[] melt = new float[symbols.Length];
        float[] boil = new float[symbols.Length];

        for (int i = 0; i < symbols.Length; i++)
        {
            string g = groups[i];

            if (g == "metal")
            {
                melt[i] = 400f;     // 金属は高融点（ざっくり）
                boil[i] = 2500f;
            }
            else if (g == "metalloid")
            {
                melt[i] = 600f;
                boil[i] = 1800f;
            }
            else if (g == "nonmetal")
            {
                melt[i] = -50f;     // 固体非金属：融点低め
                boil[i] = 200f;
            }
            else if (g == "halogen")
            {
                melt[i] = -100f;
                boil[i] = 60f;
            }
            else if (g == "noble")
            {
                melt[i] = -200f;
                boil[i] = -150f;
            }
            else if (g == "lanact")
            {
                melt[i] = 900f;
                boil[i] = 3000f;
            }
        }

        // ───────────────────────────────────────────
        // Database に適用
        // ───────────────────────────────────────────
        db.Symbols = symbols;
        db.Groups = groups;
        db.Colors = colors;
        db.MeltingPoints = melt;
        db.BoilingPoints = boil;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log("ChemElementDatabase：118元素データを自動入力しました！");
    }
}
