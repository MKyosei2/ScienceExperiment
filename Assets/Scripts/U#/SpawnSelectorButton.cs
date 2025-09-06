using UdonSharp;
using UnityEngine;

/// <summary>
/// 3Dオブジェクト自体が「押せるボタン」になり、押すと指定ゾーンに Prefab を1つだけ生成して選択へ反映する。
/// - UdonSharp の Interact() で押下を拾う（VRChat のデフォルトの「Use」インプット）
/// - UIボタン等からも呼べるように public void Press() を用意
/// </summary>
public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("What to spawn")]
    public GameObject prefab;           // 生成するプレハブ
    public Transform zone;              // 生成位置（親）
    public bool replaceExisting = true; // 既存を消して1個にする

    [Header("Selection")]
    public ESelectorCategory category = ESelectorCategory.Element; // 生成したものをどのカテゴリとして選択に反映するか
    public SelectedObjectHolder selected;                          // 選択の受け皿（任意だが設定推奨）

    [Header("Optional Feedback")]
    public AudioSource clickSfx;         // 押下音を鳴らすなら割当て
    public Animator pressAnimator;       // 押し込みアニメがあるなら割当て
    public string pressTriggerName = "Press";

    // VRChatの「Use」操作で呼ばれる
    public override void Interact()
    {
        Press();
    }

    // UIボタンなどからも使える汎用呼び出し口
    public void Press()
    {
        // 演出
        if (clickSfx != null) clickSfx.Play();
        if (pressAnimator != null && !string.IsNullOrEmpty(pressTriggerName)) pressAnimator.SetTrigger(pressTriggerName);

        // 生成
        GameObject spawned = SpawnIntoZone();
        if (spawned == null) return;

        // 選択へ反映
        ApplySelection(spawned);
    }

    private GameObject SpawnIntoZone()
    {
        if (prefab == null)
        {
            Debug.LogWarning("[SpawnSelectorButton] prefab not set.");
            return null;
        }
        if (zone == null)
        {
            Debug.LogWarning("[SpawnSelectorButton] zone not set.");
            return null;
        }

        if (replaceExisting)
        {
            for (int i = zone.childCount - 1; i >= 0; i--)
            {
                GameObject child = zone.GetChild(i).gameObject;
                GameObject.Destroy(child);
            }
        }

        GameObject go = GameObject.Instantiate(prefab, zone.position, zone.rotation, zone);
        go.name = category.ToString() + "-" + prefab.name;
        return go;
    }

    private void ApplySelection(GameObject go)
    {
        if (selected == null || go == null) return;

        if (category == ESelectorCategory.Element) selected.SetElement(go);
        else if (category == ESelectorCategory.Tool) selected.SetTool(go);
        else selected.SetCondition(go);
    }
}
