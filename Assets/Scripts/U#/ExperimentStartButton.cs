using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentStartButton : UdonSharpBehaviour
{
    public UdonSharpBehaviour experimentController;
    public StatusTextUI statusTextUI;
    public SelectedObjectHolder holder;

    public override void Interact()
    {
        Debug.Log("🧪 ExperimentStartButton: Interact() 実行");
        Debug.Log($"experimentController is {(experimentController == null ? "NULL" : "SET")}");
        if (holder == null)
        {
            Debug.LogError("❌ ExperimentStartButton: holder が設定されていません");
            ShowStatus("❌ 選択管理オブジェクトが未設定です");
            return;
        }

        if (holder.selectedElementIDs.Length == 0)
        {
            ShowStatus("⚠️ Element が選択されていません。");
            return;
        }
        if (holder.selectedToolIDs.Length == 0)
        {
            ShowStatus("⚠️ Tool が選択されていません。");
            return;
        }

        if (experimentController != null)
        {
            experimentController.SendCustomEvent("StartExperiment");
            ShowStatus("🧪 実験を開始しました。");
            Debug.Log("🧪 ExperimentControllerにStartExperimentイベント送信");
        }
        else
        {
            ShowStatus("❌ 実験コントローラーが未設定です。");
        }
    }

    private void ShowStatus(string msg)
    {
        if (statusTextUI != null)
        {
            statusTextUI.Show(msg);
        }
        Debug.Log($"📝 Status: {msg}");
    }
}
