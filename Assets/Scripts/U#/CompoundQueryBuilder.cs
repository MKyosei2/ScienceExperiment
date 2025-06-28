using UdonSharp;
using UnityEngine;

public class CompoundQueryBuilder : UdonSharpBehaviour
{
    [Tooltip("ワールドのバージョン番号（AI ロジック切替用）")]
    public string worldVersion = "1.0.0";

    public string BuildQuery(string[] elements, string conditionKey)
    {
        string elementPart = string.Join(",", elements);
        return $"e={elementPart}&c={conditionKey}&v={worldVersion}";
    }
}