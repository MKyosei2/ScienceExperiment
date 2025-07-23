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

    public override void Interact()
    {
        Debug.Log("🧪 ExperimentStartButton: Interact() 実行");

        if (!HasChild(elementZone))
        {
            ShowStatus("Element がありません。");
            return;
        }

        if (!HasChild(toolZone))
        {
            ShowStatus("Tool がありません。");
            return;
        }

        if (experimentController != null)
        {
            experimentController.SendCustomEvent("StartExperiment");
            ShowStatus("実験を開始しました。");
        }
        else
        {
            ShowStatus("実験コントローラーが未設定です。");
        }
    }

    private bool HasChild(Transform zone)
    {
        return zone != null && zone.childCount > 0;
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
