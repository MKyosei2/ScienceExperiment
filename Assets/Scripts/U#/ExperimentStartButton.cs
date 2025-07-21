using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentStartButton : UdonSharpBehaviour
{
    [Header("実験に必要な要素")]
    public GameObject elementObject;
    public GameObject toolObject;
    public GameObject conditionObject;

    [Header("連携スクリプト")]
    public UdonBehaviour experimentController;
    public UdonBehaviour statusTextUI;

    public override void Interact()
    {
        // チェック：Element, Tool, Condition 全て揃っているか？
        if (elementObject == null || !elementObject.activeInHierarchy)
        {
            ShowStatus("Element がありません。");
            return;
        }

        if (toolObject == null || !toolObject.activeInHierarchy)
        {
            ShowStatus("Tool がありません。");
            return;
        }

        if (conditionObject == null || !conditionObject.activeInHierarchy)
        {
            ShowStatus("Condition がありません。");
            return;
        }

        // 実験開始イベント送信
        if (experimentController != null)
        {
            experimentController.SendCustomEvent("StartExperiment");
        }

        ShowStatus("実験を開始しました。");
    }

    private void ShowStatus(string message)
    {
        if (statusTextUI != null)
        {
            statusTextUI.SetProgramVariable("statusText", message);
            statusTextUI.SendCustomEvent("ShowStatus");
        }
    }
}
