using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 選択状態の集約。従来API互換のため、IDベース(旧)とGameObjectベース(新)の両方を保持します。
/// </summary>
public class SelectedObjectHolder : MonoBehaviour
{
    // 新：GameObjectベース
    [SerializeField] private GameObject element;
    [SerializeField] private GameObject tool;
    [SerializeField] private GameObject condition;

    // 旧：IDベース互換（ResultReceiverや既存UIが参照してもOK）
    public List<string> selectedElementIDs = new List<string>();
    public List<string> selectedToolIDs = new List<string>();
    public string selectedConditionID;

    public GameObject Element => element;
    public GameObject Tool => tool;
    public GameObject Condition => condition;

    // —— 新API（推奨） ——
    public void SetElement(GameObject go) { element = go; }
    public void SetTool(GameObject go) { tool = go; }
    public void SetCondition(GameObject go) { condition = go; }

    /// <summary>カテゴリ不明の時の簡易設定（優先順位は適宜変更可）</summary>
    public void SetAny(GameObject go)
    {
        if (element == null) element = go;
        else if (tool == null) tool = go;
        else condition = go;
    }

    // —— 旧API互換（Add* / string引数） ——
    public void AddElement(GameObject go) => SetElement(go);
    public void AddTool(GameObject go) => SetTool(go);

    public void AddElement(string id)
    {
        if (!string.IsNullOrEmpty(id) && !selectedElementIDs.Contains(id))
            selectedElementIDs.Add(id);
    }
    public void AddTool(string id)
    {
        if (!string.IsNullOrEmpty(id) && !selectedToolIDs.Contains(id))
            selectedToolIDs.Add(id);
    }
    public void SetCondition(string id) => selectedConditionID = id;

    /// <summary>最低要件：いずれかが指定されていればOK（必要に応じて強化）</summary>
    public bool IsValid()
    {
        return element || tool || condition ||
               selectedElementIDs.Count > 0 || selectedToolIDs.Count > 0 || !string.IsNullOrEmpty(selectedConditionID);
    }

    public string ToSummaryString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Element:   {(element ? element.name : (selectedElementIDs.Count > 0 ? string.Join(",", selectedElementIDs) : "-"))}");
        sb.AppendLine($"Tool:      {(tool ? tool.name : (selectedToolIDs.Count > 0 ? string.Join(",", selectedToolIDs) : "-"))}");
        sb.AppendLine($"Condition: {(condition ? condition.name : (!string.IsNullOrEmpty(selectedConditionID) ? selectedConditionID : "-"))}");
        return sb.ToString();
    }

    /// <summary>AI向け：両系統（名前/ID）を含めた簡易JSON</summary>
    public string ToJsonPayload()
    {
        string esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\"", "\\\"");
        string arr(System.Collections.Generic.List<string> ids)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("\"").Append(esc(ids[i])).Append("\"");
            }
            sb.Append("]");
            return sb.ToString();
        }

        return "{"
            + $"\"elementName\":\"{esc(element ? element.name : null)}\","
            + $"\"toolName\":\"{esc(tool ? tool.name : null)}\","
            + $"\"conditionName\":\"{esc(condition ? condition.name : null)}\","
            + $"\"selectedElementIDs\":{arr(selectedElementIDs)},"
            + $"\"selectedToolIDs\":{arr(selectedToolIDs)},"
            + $"\"selectedConditionID\":\"{esc(selectedConditionID)}\""
            + "}";
    }
}
