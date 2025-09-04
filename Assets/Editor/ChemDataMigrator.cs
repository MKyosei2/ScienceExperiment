// Assets/Editor/ChemDataMigrator.cs
// Unity 2020+ 目安。YAML(.asset)テキストを直接パースして ChemObjectData を生成します。

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ChemDataMigrator : EditorWindow
{
    [Serializable]
    private class Candidate
    {
        public string assetPath;        // 元 .asset のパス
        public string name;             // m_Name or displayName
        public string id;
        public string displayName;      // 優先してこちら
        public string prefabGuid;       // displayPrefab の GUID
        public string symbol;
        public int? atomicNumber;
        public int? group;
        public int? period;

        public ChemObjectData.ChemType inferredType = ChemObjectData.ChemType.Tool; // デフォルト救済
        public string reason;           // 型推定の根拠
    }

    private string _sourceFolder = "Assets";
    private string _targetFolder = "Assets/Chemistry/ChemObjectData";
    private Vector2 _scroll;
    private List<Candidate> _cands = new List<Candidate>();

    [MenuItem("Tools/Chemistry/Migrate Orphaned Chem Data")]
    public static void Open()
    {
        var w = GetWindow<ChemDataMigrator>("Chem Data Migrator");
        w.minSize = new Vector2(520, 420);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ChemObjectData Migration (from orphaned .asset YAML)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        _sourceFolder = EditorGUILayout.TextField("Source Folder", _sourceFolder);
        if (GUILayout.Button("Pick", GUILayout.Width(60)))
        {
            var picked = EditorUtility.OpenFolderPanel("Pick Source Folder (under project)", Application.dataPath, "");
            if (!string.IsNullOrEmpty(picked))
            {
                // Convert absolute to relative (Assets/...)
                var proj = Application.dataPath.Replace("/Assets", "");
                if (picked.StartsWith(proj))
                    _sourceFolder = "Assets" + picked.Substring(proj.Length);
                else
                    EditorUtility.DisplayDialog("Invalid", "Pick a folder under this project.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);
        if (GUILayout.Button("Pick", GUILayout.Width(60)))
        {
            var picked = EditorUtility.OpenFolderPanel("Pick Target Folder (under project)", Application.dataPath, "");
            if (!string.IsNullOrEmpty(picked))
            {
                var proj = Application.dataPath.Replace("/Assets", "");
                if (picked.StartsWith(proj))
                    _targetFolder = "Assets" + picked.Substring(proj.Length);
                else
                    EditorUtility.DisplayDialog("Invalid", "Pick a folder under this project.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan", GUILayout.Height(28)))
        {
            Scan();
        }
        using (new EditorGUI.DisabledScope(_cands.Count == 0))
        {
            if (GUILayout.Button("Migrate All", GUILayout.Height(28)))
            {
                EnsureFolder(_targetFolder);
                int ok = 0, ng = 0;
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var c in _cands)
                    {
                        if (MigrateOne(c)) ok++; else ng++;
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                EditorUtility.DisplayDialog("Done", $"Migrated: {ok}\nFailed: {ng}", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Candidates: {_cands.Count}");

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var c in _cands)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(Path.GetFileName(c.assetPath), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Path", c.assetPath);
            EditorGUILayout.LabelField("ID", c.id ?? "(none)");
            EditorGUILayout.LabelField("DisplayName", c.displayName ?? c.name ?? "(none)");
            EditorGUILayout.LabelField("Prefab GUID", string.IsNullOrEmpty(c.prefabGuid) ? "(none)" : c.prefabGuid);
            EditorGUILayout.LabelField("Type (inferred)", $"{c.inferredType}  —  {c.reason}");
            if (c.inferredType == ChemObjectData.ChemType.Element)
            {
                EditorGUILayout.LabelField("ElementExtra",
                    $"Z={c.atomicNumber?.ToString() ?? "-"}, group={c.group?.ToString() ?? "-"}, period={c.period?.ToString() ?? "-"}, symbol={c.symbol ?? "-"}");
            }
            if (GUILayout.Button("Migrate This"))
            {
                EnsureFolder(_targetFolder);
                if (MigrateOne(c))
                {
                    EditorUtility.DisplayDialog("OK", $"Migrated: {Path.GetFileName(c.assetPath)}", "Close");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Failed: {Path.GetFileName(c.assetPath)}", "Close");
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _cands.Clear();

        if (!AssetDatabase.IsValidFolder(_sourceFolder))
        {
            EditorUtility.DisplayDialog("Invalid", $"Folder not found: {_sourceFolder}", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { _sourceFolder });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;

            var text = File.ReadAllText(path, Encoding.UTF8);
            // 必須: ScriptableObject の YAML であること
            if (!text.Contains("!u!114") && !text.Contains("MonoBehaviour")) continue;

            // id / displayName / m_Name などを拾う
            var cand = ParseCandidate(path, text);
            if (cand != null) _cands.Add(cand);
        }
        Repaint();
    }

    private Candidate ParseCandidate(string assetPath, string yaml)
    {
        var c = new Candidate { assetPath = assetPath };

        c.displayName = FindScalar(yaml, @"\bdisplayName\s*:\s*(.+)");
        c.id = FindScalar(yaml, @"\bid\s*:\s*(.+)");
        c.name = FindScalar(yaml, @"\bm_Name\s*:\s*(.+)");
        c.symbol = FindScalar(yaml, @"\bsymbol\s*:\s*(.+)");

        c.atomicNumber = FindInt(yaml, @"\batomicNumber\s*:\s*([0-9]+)");
        c.group = FindInt(yaml, @"\bgroup\s*:\s*([0-9]+)");
        c.period = FindInt(yaml, @"\bperiod\s*:\s*([0-9]+)");

        c.prefabGuid = FindPrefabGuid(yaml, @"\bdisplayPrefab\s*:\s*\{[^}]*guid:\s*([0-9a-fA-F]+)[^}]*\}");

        // 型推定
        var reasons = new List<string>();
        if (c.atomicNumber.HasValue || c.group.HasValue || c.period.HasValue || !string.IsNullOrEmpty(c.symbol))
        {
            c.inferredType = ChemObjectData.ChemType.Element;
            reasons.Add("has element fields");
        }
        else
        {
            var lower = assetPath.ToLowerInvariant();
            if (lower.Contains("tool"))
            {
                c.inferredType = ChemObjectData.ChemType.Tool;
                reasons.Add("path contains 'tool'");
            }
            if (lower.Contains("condition"))
            {
                c.inferredType = ChemObjectData.ChemType.Condition;
                reasons.Add("path contains 'condition'");
            }
            if (lower.Contains("element"))
            {
                c.inferredType = ChemObjectData.ChemType.Element;
                reasons.Add("path contains 'element'");
            }

            // ファイル名からも見る
            var fn = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            if (fn.StartsWith("tool_")) { c.inferredType = ChemObjectData.ChemType.Tool; reasons.Add("name startswith 'tool_'"); }
            else if (fn.StartsWith("cond_") || fn.Contains("condition")) { c.inferredType = ChemObjectData.ChemType.Condition; reasons.Add("name suggests condition"); }
            else if (fn.StartsWith("elem_") || fn.Contains("element")) { c.inferredType = ChemObjectData.ChemType.Element; reasons.Add("name suggests element"); }
        }
        if (reasons.Count == 0) reasons.Add("default fallback");
        c.reason = string.Join(", ", reasons);

        // 最低限の識別が無ければ除外
        if (string.IsNullOrEmpty(c.id) && string.IsNullOrEmpty(c.displayName) && string.IsNullOrEmpty(c.name))
            return null;

        return c;
    }

    private static string FindScalar(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        if (!m.Success) return null;
        var raw = m.Groups[1].Value.Trim();
        // 先頭が ' or " なら外す
        if (raw.Length >= 2 && ((raw[0] == '"' && raw[^1] == '"') || (raw[0] == '\'' && raw[^1] == '\'')))
            raw = raw.Substring(1, raw.Length - 2);
        return raw;
    }

    private static int? FindInt(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        if (!m.Success) return null;
        if (int.TryParse(m.Groups[1].Value, out var v)) return v;
        return null;
    }

    private static string FindPrefabGuid(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        if (!m.Success) return null;
        return m.Groups[1].Value.Trim();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var parts = folder.Split('/');
        var path = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = path + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(path, parts[i]);
            path = next;
        }
    }

    private bool MigrateOne(Candidate c)
    {
        try
        {
            // 生成先のファイル名を決める
            var safeName = MakeSafeFileName(c.displayName ?? c.name ?? Path.GetFileNameWithoutExtension(c.assetPath));
            var outPath = $"{_targetFolder}/{safeName}.asset";
            outPath = AssetDatabase.GenerateUniqueAssetPath(outPath);

            var data = ScriptableObject.CreateInstance<ChemObjectData>();
            data.id = string.IsNullOrEmpty(c.id) ? safeName : c.id;
            data.displayName = c.displayName ?? c.name ?? safeName;
            data.type = c.inferredType;

            // Prefab を GUID から復元
            if (!string.IsNullOrEmpty(c.prefabGuid))
            {
                var p = AssetDatabase.GUIDToAssetPath(c.prefabGuid);
                if (!string.IsNullOrEmpty(p))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (prefab) data.displayPrefab = prefab;
                }
            }

            // Element 拡張
            if (c.inferredType == ChemObjectData.ChemType.Element)
            {
                data.element.enabled = true;
                if (c.atomicNumber.HasValue) data.element.atomicNumber = c.atomicNumber.Value;
                if (c.group.HasValue) data.element.group = c.group.Value;
                if (c.period.HasValue) data.element.period = c.period.Value;
                if (!string.IsNullOrEmpty(c.symbol)) data.element.symbol = c.symbol;
            }
            else
            {
                data.element.enabled = false;
            }

            AssetDatabase.CreateAsset(data, outPath);
            EditorUtility.SetDirty(data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChemDataMigrator] Failed: {c.assetPath}\n{ex}");
            return false;
        }
    }

    private static string MakeSafeFileName(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            s = s.Replace(ch, '_');
        return s.Trim();
    }
}
