using UdonSharp;
using UnityEngine;

public class SelectorObject : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;

    public SelectionCategory category = SelectionCategory.Element;

    // 選択ID（元素記号、器具名、条件名）
    public string selectionID = "";

    public override void Interact()
    {
        if (holder == null)
        {
            Debug.LogError("[SelectorObject] holder が設定されていません");
            return;
        }

        bool ok = holder.AddSelection(category, selectionID);

        if (!ok)
            Debug.LogWarning("[SelectorObject] AddSelection に失敗（上限など）");
        else
            Debug.Log("[SelectorObject] 選択: " + category + " → " + selectionID);
    }
}
