using UdonSharp;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExperimentTableTrigger : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public ESelectorCategory category = ESelectorCategory.Element;
    public bool useObjectNameAsId = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!selected || other == null) return;

        if (useObjectNameAsId)
        {
            var id = other.gameObject.name;
            if (category == ESelectorCategory.Element) selected.AddElement(id);
            else if (category == ESelectorCategory.Tool) selected.AddTool(id);
            else selected.SetCondition(id);
        }
        else
        {
            var go = other.gameObject;
            if (category == ESelectorCategory.Element) selected.SetElement(go);
            else if (category == ESelectorCategory.Tool) selected.SetTool(go);
            else selected.SetCondition(go);
        }
    }
}
