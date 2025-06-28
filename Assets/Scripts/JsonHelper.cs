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