using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// AIに分子情報を送信し、結合種類を判定してChemElementSpawnerに返す
/// JSON形式で bonds を一括受け取り、SpawnerにApplyBondUpdateを渡す
/// </summary>
public class AIRequestSender : UdonSharpBehaviour
{
    private ChemElementSpawner currentSpawner;

    /// <summary>
    /// Orchestratorから呼ばれるエントリーポイント
    /// </summary>
    public void Run(string moleculeJson, ChemElementSpawner spawner)
    {
        SendMoleculeRequest(moleculeJson, spawner);
    }

    /// <summary>
    /// Spawnerから呼ばれる: JSONをAIに送信
    /// （現状はテスト用のダミー応答を生成）
    /// </summary>
    public void SendMoleculeRequest(string moleculeJson, ChemElementSpawner spawner)
    {
        currentSpawner = spawner;

        Debug.Log("[AIRequestSender] Molecule JSON (send): " + moleculeJson);

        // --- AI応答をシミュレーション ---
        // 本来は外部AIにリクエストし、JSON応答を受け取る想定
        string aiResponseJson = BuildDummyResponse(moleculeJson);

        Debug.Log("[AIRequestSender] Molecule JSON (recv): " + aiResponseJson);

        // JSONを解析してSpawnerに反映
        ParseAndApply(aiResponseJson);
    }

    /// <summary>
    /// ダミーAI応答を生成（将来的に外部AIに置き換え）
    /// </summary>
    private string BuildDummyResponse(string inputJson)
    {
        // 例: H2O → 水分子, CO → 二重結合, それ以外は単結合ランダム
        if (inputJson.Contains("\"H\"") && inputJson.Contains("\"O\""))
        {
            return "{ \"bonds\":[ {\"a\":0,\"b\":1,\"type\":1}, {\"a\":0,\"b\":2,\"type\":1} ] }";
        }
        else if (inputJson.Contains("\"C\"") && inputJson.Contains("\"O\""))
        {
            return "{ \"bonds\":[ {\"a\":0,\"b\":1,\"type\":2} ] }";
        }
        else
        {
            return "{ \"bonds\":[ {\"a\":0,\"b\":1,\"type\":1} ] }";
        }
    }

    /// <summary>
    /// JSON文字列を解析してSpawnerに反映
    /// （簡易パーサー：外部ライブラリは使用できないため文字列処理で対応）
    /// </summary>
    private void ParseAndApply(string json)
    {
        if (currentSpawner == null) return;
        if (string.IsNullOrEmpty(json)) return;

        // 例: { "bonds":[ {"a":0,"b":1,"type":2}, {"a":1,"b":2,"type":1} ] }
        string bondsSection = ExtractSection(json, "bonds");
        if (string.IsNullOrEmpty(bondsSection)) return;

        string[] entries = bondsSection.Split('}');
        foreach (string entry in entries)
        {
            if (entry.Contains("\"a\"") && entry.Contains("\"b\"") && entry.Contains("\"type\""))
            {
                int a = ExtractInt(entry, "\"a\":");
                int b = ExtractInt(entry, "\"b\":");
                int type = ExtractInt(entry, "\"type\":");

                if (a >= 0 && b >= 0 && type > 0)
                {
                    currentSpawner.ApplyBondUpdate(a, b, type);
                }
            }
        }
    }

    /// <summary>
    /// JSON内の指定キーのセクションを取り出す
    /// </summary>
    private string ExtractSection(string json, string key)
    {
        int start = json.IndexOf("\"" + key + "\"");
        if (start < 0) return "";
        start = json.IndexOf('[', start);
        int end = json.IndexOf(']', start);
        if (start < 0 || end < 0) return "";
        return json.Substring(start + 1, end - start - 1);
    }

    /// <summary>
    /// JSON部分文字列から数値を抽出
    /// </summary>
    private int ExtractInt(string src, string key)
    {
        int idx = src.IndexOf(key);
        if (idx < 0) return -1;
        idx += key.Length;
        string num = "";
        while (idx < src.Length && (char.IsDigit(src[idx]) || src[idx] == '-'))
        {
            num += src[idx];
            idx++;
        }
        int val;
        if (int.TryParse(num, out val)) return val;
        return -1;
    }
}
