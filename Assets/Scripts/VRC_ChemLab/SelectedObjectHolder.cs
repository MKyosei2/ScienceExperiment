using UdonSharp;
using UnityEngine;
using TMPro;

public class SelectedObjectHolder : UdonSharpBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;

    private const int MaxElements = 8;
    private const int MaxTools = 8;

    // 選ばれた ID を保持
    public string[] elementIDs = new string[MaxElements];
    public string[] toolIDs = new string[MaxTools];
    public string conditionID = "";

    private int elementCount = 0;
    private int toolCount = 0;

    // ============================================================
    // 選択処理：IDを保持する方式
    // ============================================================
    public bool AddSelection(SelectionCategory category, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        if (category == SelectionCategory.Element)
        {
            if (elementCount >= MaxElements) return false;
            elementIDs[elementCount] = id;
            elementCount++;
        }
        else if (category == SelectionCategory.Tool)
        {
            if (toolCount >= MaxTools) return false;
            toolIDs[toolCount] = id;
            toolCount++;
        }
        else // Condition
        {
            conditionID = id;
        }

        RefreshUI();
        return true;
    }

    // Condition を消す
    public void ClearCondition()
    {
        conditionID = "";
        RefreshUI();
    }

    // 全てリセット
    public void ClearAll()
    {
        for (int i = 0; i < MaxElements; i++) elementIDs[i] = "";
        for (int i = 0; i < MaxTools; i++) toolIDs[i] = "";

        elementCount = 0;
        toolCount = 0;
        conditionID = "";

        RefreshUI();
    }

    // 実験開始の条件チェック
    public bool IsValid()
    {
        return (elementCount >= 2 &&
                toolCount >= 1 &&
                !string.IsNullOrEmpty(conditionID));
    }

    // UI 更新
    private void RefreshUI()
    {
        if (statusText == null) return;
        statusText.text =
            $"Elements:{elementCount}  Tools:{toolCount}  Condition:{(string.IsNullOrEmpty(conditionID) ? "None" : "OK")}";
    }

    public int GetElementCount() => elementCount;
    public int GetToolCount() => toolCount;
}
