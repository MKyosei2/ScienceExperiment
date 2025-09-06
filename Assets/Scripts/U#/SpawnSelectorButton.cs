using UdonSharp;
using UnityEngine;

/// 3Dボタンを押すとプレハブを該当ゾーンにスポーンし、選択に追加する。
public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("What to spawn")]
    public GameObject prefab;

    [Header("Where to put it")]
    public Transform zone;

    [Header("Selection")]
    public SelectedObjectHolder selected;
    public SelectionCategory category = SelectionCategory.Element;

    [Header("Options")]
    public bool replaceExisting = false; // trueならゾーン内を1つに制限

    public override void Interact() { Spawn(); }

    /// SelectionActionController 互換：外部から押下相当で呼ばれる
    public void Press() { Spawn(); }

    public void Spawn()
    {
        if (prefab == null || zone == null || selected == null) return;

        if (replaceExisting)
        {
            for (int i = zone.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(zone.GetChild(i).gameObject);
            }
        }

        GameObject go = GameObject.Instantiate(prefab);
        go.transform.SetParent(zone, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        selected.AddSelection(category, go, go.name);
    }
}
