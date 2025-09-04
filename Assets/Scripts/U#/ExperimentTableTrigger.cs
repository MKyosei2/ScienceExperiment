using UnityEngine;

/// <summary>
/// テーブルに置かれたオブジェクトを選択状態に反映する簡易トリガー。
/// 旧実装の AddElement/AddTool 相当を、新APIにマッピング。
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExperimentTableTrigger : MonoBehaviour
{
    public enum Category { Element, Tool, Condition }

    [SerializeField] private SelectedObjectHolder selected;
    [SerializeField] private Category category = Category.Element;
    [SerializeField] private bool useObjectNameAsId = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!selected || !other) return;

        if (useObjectNameAsId)
        {
            var id = other.gameObject.name;
            switch (category)
            {
                case Category.Element: selected.AddElement(id); break;
                case Category.Tool: selected.AddTool(id); break;
                case Category.Condition: selected.SetCondition(id); break;
            }
        }
        else
        {
            var go = other.gameObject;
            switch (category)
            {
                case Category.Element: selected.SetElement(go); break;
                case Category.Tool: selected.SetTool(go); break;
                case Category.Condition: selected.SetCondition(go); break;
            }
        }
    }
}
