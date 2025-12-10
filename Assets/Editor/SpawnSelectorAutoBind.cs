using UnityEngine;
using UnityEditor;

public class SpawnSelectorAutoBind : EditorWindow
{
    [MenuItem("ChemLab/AutoBind Spawn Selector Buttons")]
    public static void AutoBind()
    {
        Debug.Log("=== SpawnSelector AutoBind 開始 ===");

        // シーンから参照を取得
        ChemElementSpawner spawner = FindObjectOfType<ChemElementSpawner>();
        ChemEnvironmentManager env = FindObjectOfType<ChemEnvironmentManager>();
        ChemStatusDisplay status = FindObjectOfType<ChemStatusDisplay>();

        if (spawner == null || env == null || status == null)
        {
            Debug.LogError("ChemElementSpawner / ChemEnvironmentManager / ChemStatusDisplay が見つかりません。");
            return;
        }

        // 全 SpawnSelectorButton を検索
        SpawnSelectorButton[] buttons = FindObjectsOfType<SpawnSelectorButton>();

        foreach (var btn in buttons)
        {
            Undo.RecordObject(btn, "AutoBind SpawnSelectorButton");

            btn.spawner = spawner;
            btn.env = env;
            btn.statusDisplay = status;

            // ボタンに応じてカテゴリと値を設定
            SetCategoryAndValue(btn);

            EditorUtility.SetDirty(btn);
        }

        Debug.Log("=== SpawnSelector AutoBind 完了 ===");
    }

    private static void SetCategoryAndValue(SpawnSelectorButton btn)
    {
        string name = btn.gameObject.name.ToUpper();

        // 元素（118元素対応）
        if (IsElementSymbol(name))
        {
            btn.category = ButtonCategory.Element;
            btn.value = name;
            return;
        }

        // 実験器具
        if (IsToolName(name))
        {
            btn.category = ButtonCategory.Equipment;
            btn.value = name;
            return;
        }

        // 環境（TEMP / HUM / PRES）
        if (name.Contains("TEMP"))
        {
            btn.category = ButtonCategory.Environment;
            btn.value = "TEMP";
            return;
        }

        if (name.Contains("HUM"))
        {
            btn.category = ButtonCategory.Environment;
            btn.value = "HUM";
            return;
        }

        if (name.Contains("PRES"))
        {
            btn.category = ButtonCategory.Environment;
            btn.value = "PRES";
            return;
        }

        // どれでもない
        btn.category = ButtonCategory.None;
        btn.value = "";
    }

    private static bool IsElementSymbol(string name)
    {
        string[] symbols = new string[]
        {
            "H","HE","LI","BE","B","C","N","O","F","NE",
            "NA","MG","AL","SI","P","S","CL","AR",
            "K","CA","SC","TI","V","CR","MN","FE","CO","NI","CU","ZN",
            "GA","GE","AS","SE","BR","KR",
            "RB","SR","Y","ZR","NB","MO","TC","RU","RH","PD","AG","CD",
            "IN","SN","SB","TE","I","XE",
            "CS","BA","LA","CE","PR","ND","PM","SM","EU","GD","TB","DY","HO","ER","TM","YB","LU",
            "HF","TA","W","RE","OS","IR","PT","AU","HG",
            "TL","PB","BI","PO","AT","RN",
            "FR","RA","AC","TH","PA","U","NP","PU","AM","CM","BK","CF","ES","FM","MD","NO","LR",
            "RF","DB","SG","BH","HS","MT","DS","RG","CN","NH","FL","MC","LV","TS","OG"
        };

        foreach (string e in symbols)
        {
            if (name == e) return true;
        }
        return false;
    }

    private static bool IsToolName(string name)
    {
        string[] tools =
        {
            "BEAKER",
            "CLAISEN_FLASK",
            "CONICAL_FLASK",
            "EAR-SHAPED_FLASK",
            "FLORENCE_FLASK",
            "GASBURNER",
            "KJELDAHL_FLASK",
            "RETORT_FLASK",
            "ROUND-BOTTOM_FLASK",
            "SCHLENK_FLASK",
            "SPOIT",
            "STRAUS_FLASK",
            "TEST_TUBE",
            "VOLUMETARIC_FLASK"
        };

        foreach (string t in tools)
        {
            if (name == t.Replace("-", "_").ToUpper())
                return true;
        }

        return false;
    }
}
