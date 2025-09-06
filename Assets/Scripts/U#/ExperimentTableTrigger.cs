// Assets/Scripts/U#/ExperimentTableTrigger.cs
using UdonSharp;
using UnityEngine;

/// 机などのTriggerに置いた（入ってきた）オブジェクトを選択として取り込む。
public class ExperimentTableTrigger : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public SelectionCategory category = SelectionCategory.Element;
    public Transform zoneForThisCategory; // 置かれた物を自動でこの子にぶら下げたい場合

    private void OnTriggerEnter(Collider other)
    {
        if (selected == null || other == null) return;

        GameObject go = other.gameObject;

        if (zoneForThisCategory != null)
        {
            go.transform.SetParent(zoneForThisCategory, true);
        }

        selected.AddSelection(category, go, go.name);
    }
}
