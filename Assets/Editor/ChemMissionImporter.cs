// Assets/Editor/ChemMissionImporter.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ChemMissionImporter
{
    [Serializable]
    private class MissionRow
    {
        public string title;
        public string prompt;
        public string goal; // goalProductFormula
        public int points = 1;

        public string tool; // required tool id

        public float tempMinC;
        public float tempMaxC;

        public float minHeat01;
        public float minStir01;
        public float minPour01;
        public float minShake01;

        public int requireComplete = 1;
    }

    [Serializable]
    private class MissionList
    {
        public MissionRow[] missions;
    }

    [MenuItem("Tools/VRC ChemLab/Import Missions (CSV or JSON)")]
    public static void ImportMissionsFromSelectedTextAsset()
    {
        var ta = Selection.activeObject as TextAsset;
        if (ta == null)
        {
            EditorUtility.DisplayDialog("ChemMissionImporter", "ProjectビューでCSV/JSONのTextAssetを選択してから実行してください。", "OK");
            return;
        }

        var orchestrator = UnityEngine.Object.FindObjectOfType<ExperimentOrchestrator>();
        if (orchestrator == null)
        {
            EditorUtility.DisplayDialog("ChemMissionImporter", "シーン内に ExperimentOrchestrator が見つかりません。", "OK");
            return;
        }

        string text = ta.text ?? "";
        MissionRow[] rows = null;

        try
        {
            rows = ParseTextToMissions(text);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("ChemMissionImporter", "解析に失敗しました:\n" + e.Message, "OK");
            return;
        }

        if (rows == null || rows.Length == 0)
        {
            EditorUtility.DisplayDialog("ChemMissionImporter", "ミッションが0件でした。CSV/JSONの内容を確認してください。", "OK");
            return;
        }

        Undo.RecordObject(orchestrator, "Import Chem Missions");

        // --- Build arrays ---
        int n = rows.Length;
        var titles = new string[n];
        var prompts = new string[n];
        var goals = new string[n];
        var points = new int[n];

        var requiredTool = new string[n];
        var tMin = new float[n];
        var tMax = new float[n];

        var minHeat = new float[n];
        var minStir = new float[n];
        var minPour = new float[n];
        var minShake = new float[n];

        var requireComplete = new int[n];

        for (int i = 0; i < n; i++)
        {
            var r = rows[i] ?? new MissionRow();

            titles[i] = r.title ?? "";
            prompts[i] = r.prompt ?? "";
            goals[i] = r.goal ?? "";
            points[i] = Mathf.Max(1, r.points);

            requiredTool[i] = r.tool ?? "";

            tMin[i] = r.tempMinC;
            tMax[i] = r.tempMaxC;

            minHeat[i] = Mathf.Clamp01(r.minHeat01);
            minStir[i] = Mathf.Clamp01(r.minStir01);
            minPour[i] = Mathf.Clamp01(r.minPour01);
            minShake[i] = Mathf.Clamp01(r.minShake01);

            requireComplete[i] = r.requireComplete != 0 ? 1 : 0;
        }

        // --- Apply ---
        orchestrator.missionTitles = titles;
        orchestrator.missionPrompts = prompts;
        orchestrator.missionGoalProductFormula = goals;
        orchestrator.missionPoints = points;

        orchestrator.missionRequiredToolId = requiredTool;
        orchestrator.missionMinTempC = tMin;
        orchestrator.missionMaxTempC = tMax;

        orchestrator.missionMinHeat01 = minHeat;
        orchestrator.missionMinStir01 = minStir;
        orchestrator.missionMinPour01 = minPour;
        orchestrator.missionMinShake01 = minShake;

        orchestrator.missionRequireComplete = requireComplete;

        EditorUtility.SetDirty(orchestrator);
        EditorSceneManager.MarkSceneDirty(orchestrator.gameObject.scene);

        EditorUtility.DisplayDialog("ChemMissionImporter", $"Import完了: {n} missions\n対象: {orchestrator.name}", "OK");
    }

    private static MissionRow[] ParseTextToMissions(string text)
    {
        string t = text.TrimStart();
        if (t.StartsWith("{") || t.StartsWith("["))
        {
            return ParseJson(text);
        }
        return ParseCsv(text);
    }

    private static MissionRow[] ParseJson(string json)
    {
        string trimmed = (json ?? "").Trim();
        if (trimmed.StartsWith("["))
        {
            // JsonUtility は配列ルート不可なのでラップ
            trimmed = "{\"missions\":" + trimmed + "}";
        }

        var list = JsonUtility.FromJson<MissionList>(trimmed);
        if (list == null || list.missions == null) return new MissionRow[0];
        return list.missions;
    }

    // CSV columns (recommended):
    // title,prompt,goal,points,tool,tempMinC,tempMaxC,minHeat01,minStir01,minPour01,minShake01,requireComplete
    private static MissionRow[] ParseCsv(string csv)
    {
        var rows = new List<MissionRow>();
        var lines = SplitLines(csv);

        if (lines.Count == 0) return rows.ToArray();

        // header
        var header = ParseCsvLine(lines[0]);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            string key = (header[i] ?? "").Trim();
            if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key)) map.Add(key, i);
        }

        for (int li = 1; li < lines.Count; li++)
        {
            string line = lines[li];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.TrimStart().StartsWith("#")) continue;

            var cols = ParseCsvLine(line);
            var r = new MissionRow();

            r.title = Get(cols, map, "title");
            r.prompt = Get(cols, map, "prompt");
            r.goal = FirstNonEmpty(
                Get(cols, map, "goal"),
                Get(cols, map, "goalProductFormula"),
                Get(cols, map, "goal_formula")
            );

            r.points = GetInt(cols, map, "points", 1);
            r.tool = FirstNonEmpty(
                Get(cols, map, "tool"),
                Get(cols, map, "requiredToolId"),
                Get(cols, map, "required_tool")
            );

            r.tempMinC = GetFloat(cols, map, "tempMinC", 0f);
            r.tempMaxC = GetFloat(cols, map, "tempMaxC", 0f);

            r.minHeat01 = GetFloat(cols, map, "minHeat01", 0f);
            r.minStir01 = GetFloat(cols, map, "minStir01", 0f);
            r.minPour01 = GetFloat(cols, map, "minPour01", 0f);
            r.minShake01 = GetFloat(cols, map, "minShake01", 0f);

            r.requireComplete = GetInt(cols, map, "requireComplete", 1);

            // skip fully empty row
            if (string.IsNullOrEmpty(r.title) &&
                string.IsNullOrEmpty(r.prompt) &&
                string.IsNullOrEmpty(r.goal) &&
                string.IsNullOrEmpty(r.tool))
            {
                continue;
            }

            rows.Add(r);
        }

        return rows.ToArray();
    }

    private static List<string> SplitLines(string text)
    {
        var list = new List<string>();
        using (var sr = new StringReader(text ?? ""))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                // ignore BOM in first line
                if (list.Count == 0) line = line.TrimStart('\uFEFF');
                list.Add(line);
            }
        }
        return list;
    }

    // Minimal CSV parser with quotes
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) { result.Add(""); return result; }

        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    // escaped quote
                    cur.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (!inQuotes && c == ',')
            {
                result.Add(cur.ToString());
                cur.Length = 0;
                continue;
            }

            cur.Append(c);
        }

        result.Add(cur.ToString());
        return result;
    }

    private static string Get(List<string> cols, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out int idx)) return "";
        if (idx < 0 || idx >= cols.Count) return "";
        return (cols[idx] ?? "").Trim();
    }

    private static int GetInt(List<string> cols, Dictionary<string, int> map, string key, int def)
    {
        string s = Get(cols, map, key);
        if (string.IsNullOrEmpty(s)) return def;
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) return v;
        if (int.TryParse(s, out v)) return v;
        return def;
    }

    private static float GetFloat(List<string> cols, Dictionary<string, int> map, string key, float def)
    {
        string s = Get(cols, map, key);
        if (string.IsNullOrEmpty(s)) return def;

        // allow "25.0" or "25,0" depending locale
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return v;
        if (float.TryParse(s, out v)) return v;
        return def;
    }

    private static string FirstNonEmpty(params string[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (!string.IsNullOrEmpty(arr[i])) return arr[i];
        }
        return "";
    }
}
