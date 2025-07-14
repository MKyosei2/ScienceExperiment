using System;
using UnityEngine;

public static class JsonHelper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    public static string ToJson<T>(T[] array, bool pretty = false)
    {
        return JsonUtility.ToJson(new Wrapper<T> { Items = array }, pretty);
    }

    public static T[] FromJson<T>(string json)
    {
        return JsonUtility.FromJson<Wrapper<T>>(json).Items;
    }
}

// 仮再現対応：Bot応答なし時のFallbackロジック例（ResultReceiver等に組込想定）
public static class BotFallbackHelper
{
    public static bool TryFallbackFromHistory(ExperimentHistory history, string eID, string tID, string cID, out string result, out string trivia)
    {
        result = "";
        trivia = "";

        if (history == null) return false;

        for (int i = 0; i < history.count; i++)
        {
            if (history.elementIDs[i] == eID &&
                history.toolIDs[i] == tID &&
                history.conditionIDs[i] == cID)
            {
                result = history.resultTexts[i];
                trivia = history.triviaTexts[i];
                return true;
            }
        }

        return false;
    }
}