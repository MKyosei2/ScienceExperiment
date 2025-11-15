#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ChemElementSpawner の elementVisualChildName を
/// 一括で設定する「一度きり」の修正スクリプト。
///
/// ※ 実行後は消してしまってOK。
/// ※ Hierarchy には一切触れず、フィールド値だけ変更します。
/// </summary>
public static class ChemElementSpawnerFixOnce
{
    // ★★ ここを自分の環境に合わせて書き換えてください ★★
    //
    // CONICAL_FLASK の中で「液体メッシュ」「中身」として
    // 色を変えたい子オブジェクトの名前を入れます。
    //
    // 例： "InnerLiquid" とか "Liquid" とか、実際の名前そのまま。
    private const string TargetChildName = "InnerLiquid";

    // デフォルト状態のときだけ上書きするための判定用
    private const string DefaultChildName = "ElementVisual";

    // ------------------------------------------------------------
    // 1. 候補名をログに出すメニュー
    // ------------------------------------------------------------
    [MenuItem("VRC Lab/一括修正/ChemElementSpawner の候補名をログ出力")]
    private static void DumpCandidateChildNames()
    {
        var spawners = Object.FindObjectsOfType<ChemElementSpawner>(true);
        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("[ChemElementSpawnerFixOnce] シーン内に ChemElementSpawner が見つかりませんでした。");
            return;
        }

        foreach (var spawner in spawners)
        {
            if (spawner == null || spawner.sourceVessel == null)
                continue;

            var transforms = spawner.sourceVessel.GetComponentsInChildren<Transform>(true);
            var names = transforms
                .Select(t => t.name)
                .Distinct()
                .OrderBy(n => n)
                .ToArray();

            Debug.Log(
                $"[ChemElementSpawnerFixOnce] Spawner='{spawner.name}' / sourceVessel='{spawner.sourceVessel.name}' の子候補名:\n" +
                string.Join(", ", names));
        }

        Debug.Log("[ChemElementSpawnerFixOnce] Console に候補名を出力しました。TargetChildName を決める参考にしてください。");
    }

    // ------------------------------------------------------------
    // 2. 実際に一括修正するメニュー（★ これを一度だけ実行 ★）
    // ------------------------------------------------------------
    [MenuItem("VRC Lab/一括修正/ChemElementSpawner の内側メッシュ名を一括設定（1回きり）")]
    private static void FixAllChemElementSpawners()
    {
        if (string.IsNullOrEmpty(TargetChildName))
        {
            EditorUtility.DisplayDialog(
                "ChemElementSpawnerFixOnce",
                "TargetChildName が空です。\nスクリプト上部の TargetChildName を、液体メッシュの名前に修正してください。",
                "OK");
            return;
        }

        var spawners = Object.FindObjectsOfType<ChemElementSpawner>(true);
        if (spawners == null || spawners.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "ChemElementSpawnerFixOnce",
                "シーン内に ChemElementSpawner が見つかりませんでした。",
                "OK");
            return;
        }

        int fixedCount = 0;
        int notFoundCount = 0;

        foreach (var spawner in spawners)
        {
            if (spawner == null || spawner.sourceVessel == null)
                continue;

            // すでに別の名前が入っている場合は触らない（手動設定を尊重）
            if (!string.IsNullOrEmpty(spawner.elementVisualChildName) &&
                spawner.elementVisualChildName != DefaultChildName)
            {
                continue;
            }

            var transforms = spawner.sourceVessel.GetComponentsInChildren<Transform>(true);
            var target = transforms.FirstOrDefault(t => t.name == TargetChildName);
            if (target == null)
            {
                Debug.LogWarning(
                    $"[ChemElementSpawnerFixOnce] Spawner='{spawner.name}' / sourceVessel='{spawner.sourceVessel.name}' に " +
                    $"名前が '{TargetChildName}' の子が見つかりませんでした。");
                notFoundCount++;
                continue;
            }

            Undo.RecordObject(spawner, "Fix ChemElementSpawner (elementVisualChildName)");
            spawner.elementVisualChildName = TargetChildName;
            EditorUtility.SetDirty(spawner);
            fixedCount++;
        }

        if (fixedCount > 0)
        {
            // シーンを Dirty にして保存対象にする
            EditorSceneManager.MarkAllScenesDirty();
        }

        EditorUtility.DisplayDialog(
            "ChemElementSpawnerFixOnce",
            $"修正完了：\n" +
            $"・更新した ChemElementSpawner: {fixedCount} 件\n" +
            $"・対象の子名が見つからなかった Spawner: {notFoundCount} 件\n\n" +
            $"※ シーン保存を忘れずに行ってください。\n" +
            $"※ もう不要なら、このスクリプトファイルは削除してOKです。",
            "OK");
    }
}
#endif
