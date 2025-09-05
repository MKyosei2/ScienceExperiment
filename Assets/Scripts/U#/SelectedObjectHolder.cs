using UdonSharp;
using UnityEngine;
using System.Text;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    [SerializeField] private GameObject element;
    [SerializeField] private GameObject tool;
    [SerializeField] private GameObject condition;

    [Header("Legacy IDs (optional)")]
    [SerializeField] private string[] elementIDs = new string[8];
    [SerializeField] private string[] toolIDs = new string[8];
    [SerializeField] private string conditionID = "";
    private int elementCount = 0;
    private int toolCount = 0;

    public GameObject Element => element;
    public GameObject Tool => tool;
    public GameObject Condition => condition;

    public void SetElement(GameObject go) { element = go; }
    public void SetTool(GameObject go) { tool = go; }
    public void SetCondition(GameObject go) { condition = go; }

    public void SetAny(GameObject go)
    {
        if (element == null) element = go;
        else if (tool == null) tool = go;
        else condition = go;
    }

    // 旧互換（ID）
    public void AddElement(string id) { AddUnique(ref elementIDs, ref elementCount, id); }
    public void AddTool(string id) { AddUnique(ref toolIDs, ref toolCount, id); }
    public void SetCondition(string id) { conditionID = string.IsNullOrEmpty(id) ? "" : id; }

    private void AddUnique(ref string[] arr, ref int count, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        for (int i = 0; i < count; i++) if (arr[i] == id) return;
        if (count < arr.Length) arr[count++] = id; else arr[arr.Length - 1] = id;
    }

    public bool IsValid()
    {
        return element != null || tool != null || condition != null ||
               elementCount > 0 || toolCount > 0 || !string.IsNullOrEmpty(conditionID);
    }

    public string ToSummaryString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Element:   " + (element ? element.name : (elementCount > 0 ? Join(elementIDs, elementCount) : "-")));
        sb.AppendLine("Tool:      " + (tool ? tool.name : (toolCount > 0 ? Join(toolIDs, toolCount) : "-")));
        sb.AppendLine("Condition: " + (condition ? condition.name : (!string.IsNullOrEmpty(conditionID) ? conditionID : "-")));
        return sb.ToString();
    }

    public string ToJsonPayload()
    {
        return "{"
            + "\"elementName\":\"" + Escape(element ? element.name : null) + "\","
            + "\"toolName\":\"" + Escape(tool ? tool.name : null) + "\","
            + "\"conditionName\":\"" + Escape(condition ? condition.name : null) + "\","
            + "\"selectedElementIDs\":" + ToJsonArray(elementIDs, elementCount) + ","
            + "\"selectedToolIDs\":" + ToJsonArray(toolIDs, toolCount) + ","
            + "\"selectedConditionID\":\"" + Escape(conditionID) + "\""
            + "}";
    }

    // ==== ヘルパ ====
    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\"", "\\\"");
    }

    private string Join(string[] arr, int count)
    {
        if (count <= 0) return "";
        var sb = new StringBuilder(arr[0]);
        for (int i = 1; i < count; i++) sb.Append(",").Append(arr[i]);
        return sb.ToString();
    }

    private string ToJsonArray(string[] arr, int count)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(",");
            var s = arr[i]; if (string.IsNullOrEmpty(s)) s = "";
            sb.Append("\"").Append(Escape(s)).Append("\"");
        }
        sb.Append("]");
        return sb.ToString();
    }
}
