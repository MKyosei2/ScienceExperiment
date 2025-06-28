using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class ElementDataImporter : MonoBehaviour
{
    [Header("JSON入力")]
    public TextAsset jsonSource;         // Resources or StreamingAssets からも可能
    public InputField inputField;        // Optional: 任意の文字列から読み込む

    [Header("表示UI")]
    public Text outputDisplay;

    private List<ElementInfo> parsedElements = new List<ElementInfo>();

    [ContextMenu("インポート実行")]
    public void ImportFromJson()
    {
        string json = "";

        if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
            json = inputField.text;
        else if (jsonSource != null)
            json = jsonSource.text;
        else
        {
            Debug.LogWarning("JSON入力がありません");
            return;
        }

        try
        {
            Dictionary<string, object>[] rawList = JsonHelper.FromJson<Dictionary<string, object>>(json);
            parsedElements.Clear();

            foreach (var dict in rawList)
            {
                ElementInfo e = new ElementInfo();

                e.name = GetSafe(dict, "name");
                e.symbol = GetSafe(dict, "symbol");
                e.atomicNumber = GetInt(dict, "atomicNumber");
                e.category = GetSafe(dict, "category");
                e.phase = GetSafe(dict, "phase");
                e.atomicMass = GetFloat(dict, "atomicMass");
                e.commonUses = GetSafe(dict, "commonUses");
                e.electronegativity = GetFloat(dict, "electronegativity");

                parsedElements.Add(e);
            }

            Debug.Log($"✅ ElementDataImporter: {parsedElements.Count} 件読み込み完了");
            ShowFirstSummary();
        }
        catch (Exception ex)
        {
            Debug.LogError("ElementDataImporter エラー: " + ex.Message);
            outputDisplay.text = "JSON読み込みに失敗しました";
        }
    }

    private void ShowFirstSummary()
    {
        if (outputDisplay == null || parsedElements.Count == 0) return;

        ElementInfo first = parsedElements[0];
        outputDisplay.text = $"[{first.symbol}] {first.name} (原子番号 {first.atomicNumber})\n分類: {first.category}, 状態: {first.phase}\n用途: {first.commonUses}";
    }

    private string GetSafe(Dictionary<string, object> dict, string key)
    {
        return dict.ContainsKey(key) ? dict[key]?.ToString() : "";
    }

    private int GetInt(Dictionary<string, object> dict, string key)
    {
        if (!dict.ContainsKey(key)) return 0;
        int.TryParse(dict[key]?.ToString(), out int val);
        return val;
    }

    private float GetFloat(Dictionary<string, object> dict, string key)
    {
        if (!dict.ContainsKey(key)) return 0f;
        float.TryParse(dict[key]?.ToString(), out float val);
        return val;
    }

    // 🔬 1要素の情報保持
    [Serializable]
    public class ElementInfo
    {
        public string name;
        public string symbol;
        public int atomicNumber;
        public string category;
        public string phase;
        public float atomicMass;
        public string commonUses;
        public float electronegativity;

        public string Summary()
        {
            return $"[{symbol}] {name} - {category}, 質量: {atomicMass} u\n用途: {commonUses}";
        }
    }
}
