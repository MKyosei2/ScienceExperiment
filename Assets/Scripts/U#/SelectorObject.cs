using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// 選択可能な元素・器具・環境オブジェクトにアタッチされる
/// 選択時に、渡された SelectedObjectHolder に自分の objectType / objectID を反映する
/// </summary>
public class SelectorObject : UdonSharpBehaviour
{
    [Tooltip("Element / Tool / Condition のいずれか")]
    [SerializeField]
    private string objectType;

    [Tooltip("ID（例: Na, beaker, vacuum など）")]
    [SerializeField]
    private string objectID;
    public string GetObjectType() => objectType;
    public string GetObjectID() => objectID;

    public void SetObjectType(string type) => objectType = type;
    public void SetObjectID(string id) => objectID = id;

    public void SetObjectTypeAndID(string type, string id)
    {
        objectType = type;
        objectID = id;
    }

    /// <summary>
    /// 選択ゾーンボタンから呼ばれる。objectTypeに応じて Holder に値を渡す。
    /// </summary>
    public void Select(SelectedObjectHolder holder)
    {
        if (holder == null) return;

        switch (objectType)
        {
            case "Element":
                holder.selectedElementID = objectID;
                break;
            case "Tool":
                holder.selectedToolID = objectID;
                break;
            case "Condition":
                holder.selectedConditionID = objectID;
                break;
        }
    }
}
