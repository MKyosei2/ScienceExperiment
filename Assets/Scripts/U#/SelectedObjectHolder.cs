using UdonSharp;
using UnityEngine;
using TMPro;

/// プレイヤーが選んだオブジェクトやIDを保持し、妥当性チェックやペイロード化を行う。
public class SelectedObjectHolder : UdonSharpBehaviour
{
    [Header("Zones (スポーン/設置先)")]
    public Transform elementZone;
    public Transform toolZone;
    public Transform conditionZone;

    [Header("UI (任意)")]
    public TextMeshProUGUI statusText;

    private const int MaxElements = 8;
    private const int MaxTools = 8;

    // 実体参照
    public GameObject[] elementObjects = new GameObject[MaxElements];
    public GameObject[] toolObjects = new GameObject[MaxTools];
    public GameObject conditionObject;

    // ID（未指定ならオブジェクト名を使う）
    public string[] elementIDs = new string[MaxElements];
    public string[] toolIDs = new string[MaxTools];
    public string conditionID = "";

    private int elementCount = 0;
    private int toolCount = 0;

    // ====== 公開API ======

    /// オブジェクトを追加/設定する。戻り値: 成功/失敗
    public bool AddSelection(SelectionCategory category, GameObject obj, string idOrName = "")
    {
        if (obj == null) return false;
        string id = string.IsNullOrEmpty(idOrName) ? obj.name : idOrName;

        if (category == SelectionCategory.Element)
        {
            if (elementCount >= MaxElements) return false;
            elementObjects[elementCount] = obj;
            elementIDs[elementCount] = id;
            elementCount++;
            RefreshUI();
            return true;
        }
        else if (category == SelectionCategory.Tool)
        {
            if (toolCount >= MaxTools) return false;
            toolObjects[toolCount] = obj;
            toolIDs[toolCount] = id;
            toolCount++;
            RefreshUI();
            return true;
        }
        else // Condition（常に1つのみ）
        {
            conditionObject = obj;
            conditionID = id;
            RefreshUI();
            return true;
        }
    }

    /// SelectionActionController 互換：ゾーンなどから渡されたGOをいい感じに振り分ける
    public void SetAny(GameObject go)
    {
        if (go == null) return;

        // 1) 親ゾーンから推定
        if (elementZone != null && go.transform.IsChildOf(elementZone))
        { AddSelection(SelectionCategory.Element, go, go.name); return; }

        if (toolZone != null && go.transform.IsChildOf(toolZone))
        { AddSelection(SelectionCategory.Tool, go, go.name); return; }

        if (conditionZone != null && go.transform.IsChildOf(conditionZone))
        { AddSelection(SelectionCategory.Condition, go, go.name); return; }

        // 2) フォールバック：足りない枠へ順に入れる
        if (elementCount < 2) { AddSelection(SelectionCategory.Element, go, go.name); return; }
        if (toolCount < 1) { AddSelection(SelectionCategory.Tool, go, go.name); return; }
        AddSelection(SelectionCategory.Condition, go, go.name);
    }

    /// Condition を明示的にクリア
    public void ClearCondition()
    {
        conditionObject = null;
        conditionID = "";
        RefreshUI();
    }

    /// すべての選択を初期化
    public void ClearAll()
    {
        for (int i = 0; i < MaxElements; i++) { elementObjects[i] = null; elementIDs[i] = ""; }
        for (int i = 0; i < MaxTools; i++) { toolObjects[i] = null; toolIDs[i] = ""; }
        elementCount = 0; toolCount = 0;
        conditionObject = null; conditionID = "";
        RefreshUI();
    }

    /// 妥当性: Element 2個以上、Tool 1個以上、Condition 1個
    public bool IsValid()
    {
        if (elementCount < 2) return false;
        if (toolCount < 1) return false;
        if (string.IsNullOrEmpty(conditionID) && conditionObject == null) return false;
        return true;
    }

    /// 実験AI/プレイヤーへ渡す簡易JSON
    public string ToJsonPayload()
    {
        string json = "{";
        json += "\"elements\":[";
        for (int i = 0; i < elementCount; i++)
        {
            json += "\"" + Escape(elementIDs[i]) + "\"";
            if (i < elementCount - 1) json += ",";
        }
        json += "],";

        json += "\"tools\":[";
        for (int i = 0; i < toolCount; i++)
        {
            json += "\"" + Escape(toolIDs[i]) + "\"";
            if (i < toolCount - 1) json += ",";
        }
        json += "],";

        string cond = !string.IsNullOrEmpty(conditionID) ? conditionID :
                      (conditionObject != null ? conditionObject.name : "");
        json += "\"condition\":\"" + Escape(cond) + "\"";
        json += "}";
        return json;
    }

    // ====== ユーティリティ ======

    private string Escape(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private void RefreshUI()
    {
        if (statusText == null) return;
        statusText.text =
            $"Elements: {elementCount} / Tools: {toolCount} / Condition: {(string.IsNullOrEmpty(conditionID) && conditionObject == null ? "None" : "OK")}";
    }

    public int GetElementCount() { return elementCount; }
    public int GetToolCount() { return toolCount; }
}
