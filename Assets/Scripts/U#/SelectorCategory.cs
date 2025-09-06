using UdonSharp;
using UnityEngine;
using TMPro;

// ===== グローバル enum =====
public enum SelectionCategory
{
    Element = 0,   // 元素（2個以上）
    Tool = 1,      // 器具（1個以上）
    Condition = 2, // 環境（1個）
}

// ===== カテゴリ制御コンポーネント =====
[AddComponentMenu("VRC Lab/SelectorCategory")]
public class SelectorCategory : UdonSharpBehaviour
{
    [Header("State")]
    public SelectionCategory current = SelectionCategory.Element;

    [Header("Propagate to")]
    public SpawnSelectorButton[] spawnButtons;
    public SelectorObject[] selectorObjects;

    [Header("Optional: UI show/hide")]
    public GameObject[] elementShow;
    public GameObject[] elementHide;
    public GameObject[] toolShow;
    public GameObject[] toolHide;
    public GameObject[] conditionShow;
    public GameObject[] conditionHide;

    [Header("Optional: Label")]
    public TextMeshProUGUI label;

    [Header("Notify listeners (SendCustomEvent \"OnCategoryChanged\")")]
    public UdonSharpBehaviour[] listeners;

    private void OnEnable() { Apply(); }

    // --- UI から呼ぶ setter ---
    public void SetElement() { current = SelectionCategory.Element; Apply(); }
    public void SetTool() { current = SelectionCategory.Tool; Apply(); }
    public void SetCondition() { current = SelectionCategory.Condition; Apply(); }
    public void SetByInt(int value)
    {
        if (value < 0) value = 0;
        if (value > 2) value = 2;
        current = (SelectionCategory)value;
        Apply();
    }
    public void Next()
    {
        int v = (int)current + 1;
        if (v > 2) v = 0;
        current = (SelectionCategory)v;
        Apply();
    }
    public void Prev()
    {
        int v = (int)current - 1;
        if (v < 0) v = 2;
        current = (SelectionCategory)v;
        Apply();
    }
    public int GetCurrentAsInt() { return (int)current; }

    // --- 反映 ---
    private void Apply()
    {
        // 1) 連携先へカテゴリ反映
        if (spawnButtons != null)
        {
            for (int i = 0; i < spawnButtons.Length; i++)
            {
                var b = spawnButtons[i];
                if (b != null) b.category = current;
            }
        }
        if (selectorObjects != null)
        {
            for (int i = 0; i < selectorObjects.Length; i++)
            {
                var s = selectorObjects[i];
                if (s != null) s.category = current;
            }
        }

        // 2) UI切替（任意）
        ToggleArray(elementShow, current == SelectionCategory.Element);
        ToggleArray(elementHide, current != SelectionCategory.Element);
        ToggleArray(toolShow, current == SelectionCategory.Tool);
        ToggleArray(toolHide, current != SelectionCategory.Tool);
        ToggleArray(conditionShow, current == SelectionCategory.Condition);
        ToggleArray(conditionHide, current != SelectionCategory.Condition);

        // 3) ラベル更新（任意）
        if (label != null)
        {
            if (current == SelectionCategory.Element) label.text = "Element";
            else if (current == SelectionCategory.Tool) label.text = "Tool";
            else label.text = "Condition";
        }

        // 4) リスナー通知（任意）
        if (listeners != null)
        {
            for (int i = 0; i < listeners.Length; i++)
            {
                var l = listeners[i];
                if (l != null) l.SendCustomEvent("OnCategoryChanged");
            }
        }
    }

    private void ToggleArray(GameObject[] arr, bool state)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            var go = arr[i];
            if (go != null) go.SetActive(state);
        }
    }
}
