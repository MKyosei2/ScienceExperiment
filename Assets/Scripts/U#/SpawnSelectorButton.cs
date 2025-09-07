using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/Spawn/SpawnSelectorButton")]
public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("What to spawn")]
    public GameObject prefab;

    [Header("Where to put it")]
    public Transform zone;

    [Header("Selection")]
    public SelectedObjectHolder selected;
    public SelectionCategory category = SelectionCategory.Element;

    [Header("Mode (optional)")]
    public ModeRouter router;                  // ← ここに Managers/ModeRouter を割り当て

    [Header("Options")]
    public bool replaceExisting = false; // trueならゾーン内を1つに制限

    public override void Interact() { Spawn(); }

    // 外部から押下相当で呼ばれる
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

        // === ここで Prefab の ModeActivation に Router を注入＆即適用 ===
        var mas = go.GetComponentsInChildren<ModeActivation>(true);
        for (int i = 0; i < mas.Length; i++)
        {
            var m = mas[i];
            if (m == null) continue;

            if (router != null)
            {
                // Routerへ登録＆現在モードを反映
                router.Register(m);
                m.ApplyModeFromRouter(router, router.IsVR());
            }
            else
            {
                // Router未設定ならスタンドアロン（自動判定）で適用
                m.ApplyStandalone();
            }
        }

        selected.AddSelection(category, go, go.name);
    }
}
