#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AssignSpawnPrefabsOnce
{
    private const string kMarkerPath = "Assets/Editor/SE_AssignPrefabsOnce.marker"; // 実行済みマーカー

    [MenuItem("Tools/SE/Assign Prefabs to Spawn Buttons (Once)")]
    public static void AssignOnceMenu()
    {
        if (File.Exists(kMarkerPath))
        {
            EditorUtility.DisplayDialog(
                "Already executed",
                "この自動割り当ては既に実行済みです。\n\n再実行したい場合は:\nTools/SE/Assign Prefabs to Spawn Buttons (Force) を使ってください。",
                "OK");
            return;
        }
        CoreAssign(logHeader: "Assign Prefabs (Once)");
        // 実行済みマーカー作成
        Directory.CreateDirectory(Path.GetDirectoryName(kMarkerPath));
        File.WriteAllText(kMarkerPath, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AssetDatabase.ImportAsset(kMarkerPath);
    }

    [MenuItem("Tools/SE/Assign Prefabs to Spawn Buttons (Force)")]
    public static void AssignForceMenu()
    {
        CoreAssign(logHeader: "Assign Prefabs (Force)");
    }

    private static void CoreAssign(string logHeader)
    {
        var buttons = Object.FindObjectsOfType<SpawnSelectorButton>(true);
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning($"[SE] {logHeader}: シーン内に SpawnSelectorButton が見つかりません。");
            return;
        }

        int assigned = 0;
        int skipped = 0;

        // まとめて検索効率化のため、全PrefabのGUID一覧を先に取得
        var allPrefabGuids = AssetDatabase.FindAssets("t:prefab");
        // name => path のマップを作る（重複名は最初のものを優先）
        var nameToPath = allPrefabGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => !string.IsNullOrEmpty(p))
            .GroupBy(p => Path.GetFileNameWithoutExtension(p))
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var b in buttons)
        {
            if (b == null) continue;

            // 既に設定済みならスキップ
            if (b.prefab != null)
            {
                skipped++;
                continue;
            }

            // 候補名生成：GameObject名から「Button/Btn/_Button」などの語尾を取り除き、さらに "Prefab" 付きも候補に
            string goName = b.gameObject.name;
            string baseName = StripSuffixes(goName);
            var candidates = new[]
            {
                baseName,
                baseName + "Prefab",
                goName,
                goName + "Prefab"
            }.Distinct().ToArray();

            string hitPath = null;

            // 1) 完全一致優先
            foreach (var cand in candidates)
            {
                if (nameToPath.TryGetValue(cand, out hitPath))
                    break;
            }

            // 2) 見つからなければ「含む」検索（カテゴリヒント付き）
            if (string.IsNullOrEmpty(hitPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:prefab " + baseName);
                if (guids != null && guids.Length > 0)
                {
                    // カテゴリヒント（Element / Tool / Condition をパスに含むものを優先）
                    string hint = b.category.ToString(); // ESelectorCategory.ToString()
                    string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

                    // ヒント含む > 名前を含む > 先頭
                    string pathByHint = paths.FirstOrDefault(p => p.ToLower().Contains(hint.ToLower()));
                    hitPath = pathByHint ?? paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).ToLower().Contains(baseName.ToLower()))
                                      ?? paths[0];
                }
            }

            if (!string.IsNullOrEmpty(hitPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(hitPath);
                if (prefab != null)
                {
                    Undo.RecordObject(b, "[SE] Assign Spawn Prefab");
                    b.prefab = prefab;
                    EditorUtility.SetDirty(b);
                    assigned++;
                    continue;
                }
            }

            // 最後まで見つからなかった
            Debug.LogWarning($"[SE] {logHeader}: Prefabが見つかりませんでした → Button='{b.name}' 候補='{baseName}'");
        }

        if (assigned > 0) AssetDatabase.SaveAssets();

        Debug.Log($"[SE] {logHeader}: Assigned={assigned}, Skipped(already set)={skipped}, TotalButtons={buttons.Length}");
    }

    private static string StripSuffixes(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        string[] suffixes = { "Button", "_Button", "-Button", "Btn", "_Btn", "-Btn" };
        foreach (var suf in suffixes)
        {
            if (s.EndsWith(suf)) return s.Substring(0, s.Length - suf.Length);
        }
        return s;
    }
}
#endif
