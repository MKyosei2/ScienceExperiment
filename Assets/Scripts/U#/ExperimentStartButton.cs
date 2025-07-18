using UdonSharp;
using UnityEngine;

public class ExperimentStartButton : UdonSharpBehaviour
{
    public ExperimentController controller;
    public ModeSwitcher modeSwitcher;
    public Transform experimentTableRoot;

    public override void Interact()
    {
        if (modeSwitcher == null || controller == null)
        {
            Debug.LogWarning("❌ 実験開始の参照が未設定です");
            return;
        }

        if (!modeSwitcher.IsPCMode())
        {
            Debug.Log("⚠️ PCモードでのみボタンは有効です");
            return;
        }

        controller.RunExperiment();
    }
}