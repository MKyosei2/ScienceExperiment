using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SelectorObject : UdonSharpBehaviour
{
    [SerializeField] private string objectType;  // "Element", "Tool", "Condition"
    [SerializeField] private string objectID;

    public SelectedObjectHolder holder;

    public string GetObjectType() => objectType;
    public string GetObjectID() => objectID;

    public void SetObjectType(string type) => objectType = type;
    public void SetObjectID(string id) => objectID = id;

    public void SetObjectTypeAndID(string type, string id)
    {
        objectType = type;
        objectID = id;
    }

    public void Select()
    {
        if (holder == null)
        {
            Debug.LogWarning("⚠️ SelectorObject: holder が未設定です。選択処理をスキップします。");
            return;
        }

        ApplySelection(holder);
    }

    public void Select(SelectedObjectHolder targetHolder)
    {
        if (targetHolder == null)
        {
            Debug.LogError("❌ SelectorObject: targetHolder が null のため選択できません。");
            return;
        }

        ApplySelection(targetHolder);
    }

    private void ApplySelection(SelectedObjectHolder target)
    {
        switch (objectType)
        {
            case "Element":
                target.AddElement(objectID);
                Debug.Log($"✅ Element {objectID} を選択しました");
                break;

            case "Tool":
                target.AddTool(objectID);
                Debug.Log($"✅ Tool {objectID} を選択しました");
                break;

            case "Condition":
                target.SetCondition(objectID);
                Debug.Log($"✅ Condition {objectID} を設定しました");
                break;

            default:
                Debug.LogWarning($"⚠️ 未知の objectType: {objectType}（正しくは 'Element', 'Tool', 'Condition'）");
                break;
        }
    }

    public override void Interact()
    {
        Select();
    }
}
