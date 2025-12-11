using UnityEngine;
using UnityEditor;

public class SpawnSelectorAutoBind : EditorWindow
{
    [Header("Assign Targets")]
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;
    public ChemStatusDisplay statusDisplay;

    [MenuItem("ChemLab/AutoBind/SpawnSelector Buttons")]
    public static void OpenWindow()
    {
        GetWindow<SpawnSelectorAutoBind>("SpawnSelector AutoBind");
    }

    private void OnGUI()
    {
        GUILayout.Label("SpawnSelectorButton 自動設定", EditorStyles.boldLabel);
        GUILayout.Space(10);

        elementSpawner = EditorGUILayout.ObjectField("Element Spawner", elementSpawner, typeof(ChemElementSpawner), true) as ChemElementSpawner;
        environmentManager = EditorGUILayout.ObjectField("Environment Manager", environmentManager, typeof(ChemEnvironmentManager), true) as ChemEnvironmentManager;
        statusDisplay = EditorGUILayout.ObjectField("Status Display", statusDisplay, typeof(ChemStatusDisplay), true) as ChemStatusDisplay;

        GUILayout.Space(15);

        if (GUILayout.Button("一括設定する", GUILayout.Height(35)))
        {
            AutoAssign();
        }
    }

    private void AutoAssign()
    {
        if (elementSpawner == null || environmentManager == null || statusDisplay == null)
        {
            Debug.LogError("すべての参照を設定してください！");
            return;
        }

        int count = 0;

        foreach (SpawnSelectorButton btn in FindObjectsOfType<SpawnSelectorButton>(true))
        {
            Undo.RecordObject(btn, "Auto Assign Spawn Selector");

            // --- ID 設定（ボタン名をそのまま ID にする）---
            btn.idOrName = btn.gameObject.name;

            // --- カテゴリを自動判定 ---
            string name = btn.gameObject.name.ToUpper();

            if (IsElement(name))
                btn.category = SelectionCategory.Element;
            else if (IsTool(name))
                btn.category = SelectionCategory.Tool;
            else if (IsCondition(name))
                btn.category = SelectionCategory.Condition;
            else
                Debug.LogWarning($"カテゴリ不明: {btn.gameObject.name}");

            // --- 共通参照を設定 ---
            btn.elementSpawner = elementSpawner;
            btn.environmentManager = environmentManager;
            btn.statusDisplay = statusDisplay;

            EditorUtility.SetDirty(btn);
            count++;
        }

        Debug.Log($"SpawnSelectorButton {count} 個を自動設定しました！");
    }

    private bool IsElement(string name)
    {
        // 元素記号は 1～2 文字、基本 A〜Z
        return name.Length <= 3 && char.IsLetter(name[0]);
    }

    private bool IsTool(string name)
    {
        return name.Contains("FLASK") ||
               name.Contains("BEAKER") ||
               name.Contains("TUBE") ||
               name.Contains("PIPETTE") ||
               name.Contains("BURNER");
    }

    private bool IsCondition(string name)
    {
        return name.Contains("TEMP") ||
               name.Contains("HUMID") ||
               name.Contains("PRESS") ||
               name.Contains("CONDITION");
    }
}
