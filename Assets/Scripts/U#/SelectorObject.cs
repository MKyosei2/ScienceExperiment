using UdonSharp;
using UnityEngine;

public class SelectorObject : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public ESelectorCategory category = ESelectorCategory.Element; // ← enum は ESelectorCategory を使用
    public string idOverride;

    public void Select()
    {
        if (selected == null) return;

        if (!string.IsNullOrEmpty(idOverride))
        {
            if (category == ESelectorCategory.Element) selected.AddElement(idOverride);
            else if (category == ESelectorCategory.Tool) selected.AddTool(idOverride);
            else selected.SetCondition(idOverride);
        }
        else
        {
            if (category == ESelectorCategory.Element) selected.SetElement(gameObject);
            else if (category == ESelectorCategory.Tool) selected.SetTool(gameObject);
            else selected.SetCondition(gameObject);
        }
    }
}
