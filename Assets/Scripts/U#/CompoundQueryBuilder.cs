using UdonSharp;
using UnityEngine;

public class CompoundQueryBuilder : UdonSharpBehaviour
{
    public string BuildQuery(string[] elements, string conditionKey)
    {
        return $"e={string.Join(",", elements)}&c={conditionKey}";
    }
}