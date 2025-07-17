using UdonSharp;
using UnityEngine;

public class ExperimentStartButton : UdonSharpBehaviour
{
    public ExperimentController controller;
    public ModeSwitcher modeSwitcher;
    public Transform experimentTableRoot;

    public override void Interact()
    {
        if (modeSwitcher == null || controller == null || experimentTableRoot == null)
        {
            Debug.LogWarning("❌ 必要な参照が設定されていません（ModeSwitcher または Controller または TableRoot）");
            return;
        }

        if (!modeSwitcher.IsPCMode())
        {
            Debug.Log("⚠️ PCモードでのみ実行可能です");
            return;
        }

        Debug.Log("🔘 実験ボタンが押されました");
        controller.CollectFromTable();  // 引数なしで呼び出す
        controller.RunExperiment();     // AIへの送信を含む
    }
}
