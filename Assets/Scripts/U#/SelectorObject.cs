// Assets/Scripts/U#/SelectorObject.cs
using UdonSharp;
using UnityEngine;

/// 触った(Interact)オブジェクトを選択として SelectedObjectHolder に登録する。
public class SelectorObject : UdonSharpBehaviour
{
    [Header("Selection")]
    public SelectedObjectHolder selected;                 // 登録先
    public SelectionCategory category = SelectionCategory.Element; // Element / Tool / Condition
    [Tooltip("空なら GameObject.name をIDとして使う")]
    public string idOverride = "";

    [Header("Optional: 選択時にゾーンへ移動")]
    public bool parentToZoneOnSelect = false;
    public Transform zoneForThisCategory;

    public override void Interact()
    {
        Select();
    }

    public void Select()
    {
        if (selected == null) return;

        if (parentToZoneOnSelect && zoneForThisCategory != null)
        {
            transform.SetParent(zoneForThisCategory, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // idOverride が空なら this.gameObject.name を使う
        selected.AddSelection(category, gameObject, idOverride);
    }
}
