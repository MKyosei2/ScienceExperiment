using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class AICompoundResponseParser : UdonSharpBehaviour
{
    public ResultDisplayManager displayManager;
    public CompoundPrefabAssembler assembler;
    public AICompoundNarrator narrator;

    public void ParseResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            displayManager?.ShowResult("AI応答が無効です");
            return;
        }

        // 仮の軽量パーサ（正規のJSON解析ライブラリはUdonで使えないため）
        string reaction = Extract(json, "\"reaction\"");
        string funFact = Extract(json, "\"funFact\"");
        string styleStr = Extract(json, "\"style\"");
        int style = 0;
        int.TryParse(styleStr, out style);

        displayManager?.ShowResult($"{reaction} が生成されました！");
        assembler?.GenerateCompound(reaction, style);
        narrator?.PlayNarration(reaction, funFact);
    }

    private string Extract(string json, string key)
    {
        int start = json.IndexOf(key);
        if (start == -1) return "";
        start = json.IndexOf(":", start) + 1;
        if (json[start] == '"') start += 1;
        int end = json.IndexOf(json[start - 1] == '"' ? "\"" : ",", start);
        return json.Substring(start, end - start).Trim();
    }
}