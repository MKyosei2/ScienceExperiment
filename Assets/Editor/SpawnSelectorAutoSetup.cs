using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpawnSelectorAutoSetup : EditorWindow
{
    private ChemElementSpawner spawner;

    // あなたのHierarchyに合わせて。既定はスクショ通り。
    private string elementParentName = "Element";
    private string equipmentParentName = "Tool";
    private string conditionParentName = "Condition";

    // 実行結果の保持
    private readonly List<Transform> _missingTransforms = new List<Transform>();
    private readonly List<string> _missingPaths = new List<string>();
    private int _setCount;

    [MenuItem("VRC ChemLab/Auto Setup/SpawnSelector Buttons")]
    public static void Open() => GetWindow<SpawnSelectorAutoSetup>("SpawnSelector Auto Setup");

    private void OnGUI()
    {
        GUILayout.Label("SpawnSelectorButton 一括設定", EditorStyles.boldLabel);

        spawner = (ChemElementSpawner)EditorGUILayout.ObjectField("Spawner", spawner, typeof(ChemElementSpawner), true);
        elementParentName = EditorGUILayout.TextField("元素 親名", elementParentName);
        equipmentParentName = EditorGUILayout.TextField("器具 親名", equipmentParentName);
        conditionParentName = EditorGUILayout.TextField("環境 親名", conditionParentName);

        GUILayout.Space(6);
        if (GUILayout.Button("⚙ 一括設定を実行"))
        {
            RunAutoSetup();
        }

        if (_missingTransforms.Count > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"未アタッチ: {_missingTransforms.Count} 件。以下ボタンで選択やレポート出力ができます。", MessageType.Warning);
            if (GUILayout.Button("👀 未アタッチをすべて選択"))
            {
                Selection.objects = _missingTransforms.ToArray();
                if (_missingTransforms[0] != null)
                    EditorGUIUtility.PingObject(_missingTransforms[0].gameObject);
            }
            if (GUILayout.Button("📝 未アタッチ一覧をファイル出力"))
            {
                var path = "Assets/Editor/SpawnSelectorButton_MissingReport.txt";
                File.WriteAllLines(path, _missingPaths.ToArray());
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("レポート作成", $"未アタッチ一覧を保存しました:\n{path}", "OK");
            }
        }

        EditorGUILayout.HelpBox(
            "・各親( Element / Tool / Condition )配下を走査し、SpawnSelectorButton を一括配線します。\n" +
            "・未アタッチのオブジェクトは“選択”や“ファイル出力”で即確認できます。\n" +
            "・Console で見たい場合は Collapse をOFF、Info/Warning をONにしてください。",
            MessageType.Info
        );
    }

    private void RunAutoSetup()
    {
        if (spawner == null)
        {
            Debug.LogError("[SpawnSelectorAutoSetup] ❌ Spawner が未設定です。ChemElementSpawner を指定してください。");
            return;
        }

        _missingTransforms.Clear();
        _missingPaths.Clear();
        _setCount = 0;

        _setCount += SetupUnder(elementParentName, "Element", SelectionCategory.Element);
        _setCount += SetupUnder(equipmentParentName, "Equipment", SelectionCategory.Tool);
        _setCount += SetupUnder(conditionParentName, "Condition", SelectionCategory.Condition);

        EditorUtility.SetDirty(spawner);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SpawnSelectorAutoSetup] ✅ 一括設定完了: 設定 {_setCount} 件, 警告 {_missingTransforms.Count} 件。");
        if (_missingTransforms.Count > 0)
        {
            Debug.LogWarning("⚠ SpawnSelectorButton 未アタッチ一覧（フルパス）:");
            for (int i = 0; i < _missingPaths.Count; i++)
                Debug.LogWarning(" - " + _missingPaths[i]);
        }

        // 実行後、ウィンドウに操作ボタンを表示するため再描画
        Repaint();
    }

    private int SetupUnder(string parentName, string type, SelectionCategory cat)
    {
        var root = GameObject.Find(parentName);
        if (root == null)
        {
            Debug.LogWarning($"[SpawnSelectorAutoSetup] 親 '{parentName}' が見つかりません。スキップ。");
            return 0;
        }

        int count = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root.transform) continue;

            var btn = t.GetComponent<SpawnSelectorButton>();
            if (btn == null)
            {
                _missingTransforms.Add(t);
                _missingPaths.Add(GetFullPath(t));
                continue;
            }

            Undo.RecordObject(btn, "AutoSetup SpawnSelectorButton");
            btn.spawner = spawner;
            btn.type = type;
            btn.targetName = t.name;
            btn.category = cat;
            btn.categoryName = TypeToCategoryName(type);
            EditorUtility.SetDirty(btn);
            count++;
        }
        return count;
    }

    private static string TypeToCategoryName(string type)
    {
        if (type == "Element") return "Element";
        if (type == "Equipment") return "Tool";
        if (type == "Condition") return "Condition";
        return type;
    }

    private static string GetFullPath(Transform t)
    {
        string path = t.name;
        for (var p = t.parent; p != null; p = p.parent)
            path = p.name + "/" + path;
        return path;
    }
}
