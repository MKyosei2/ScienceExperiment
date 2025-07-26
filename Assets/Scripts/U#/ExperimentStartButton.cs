using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentStartButton : UdonSharpBehaviour
{
    [Header("対象ゾーン（各カテゴリの生成先）")]
    public Transform elementZone;
    public Transform toolZone;

    [Header("連携スクリプト")]
    public UdonBehaviour experimentController;
    public UdonBehaviour statusTextUI;

    [Header("選択オブジェクト管理")]
    public SelectedObjectHolder holder;

    public override void Interact()
    {
        Debug.Log("🧪 ExperimentStartButton: Interact() 実行");

        if (holder == null)
        {
            Debug.LogError("❌ ExperimentStartButton: holder が設定されていません");
            ShowStatus("❌ 選択管理オブジェクトが未設定です");
            return;
        }

        Debug.Log("🧪 ExperimentStartButton.holder instance ID = " + holder.GetInstanceID());
        Debug.Log("🧪 holder.selectedElementIDs.Length = " + holder.selectedElementIDs.Length);
        Debug.Log("🧪 holder.selectedToolIDs.Length = " + holder.selectedToolIDs.Length);
        Debug.Log("🧪 holder.selectedConditionID = " + holder.selectedConditionID);

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
            statusTextUI.SetProgramVariable("statusText", msg);
            statusTextUI.SendCustomEvent("ShowStatus");
        }

        Debug.Log($"📝 Status: {msg}");
    }
}
