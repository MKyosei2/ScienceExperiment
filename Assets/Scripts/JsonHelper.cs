using UnityEngine;

public static class JsonHelper
{
    public static bool IsJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.TrimStart();
        return s.StartsWith("{") || s.StartsWith("[");
    }
}
