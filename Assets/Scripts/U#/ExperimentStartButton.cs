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
        Debug.Log("🧪 ExperimentStartButton: Interact() 呼び出し");

        if (elementObject == null || !elementObject.activeInHierarchy)
        {
            ShowStatus("Element がありません。");
            Debug.LogWarning("❌ ExperimentStartButton: Element が null または非表示です");
            return;
        }

        if (toolObject == null || !toolObject.activeInHierarchy)
        {
            ShowStatus("Tool がありません。");
            Debug.LogWarning("❌ ExperimentStartButton: Tool が null または非表示です");
            return;
        }

        if (conditionObject == null || !conditionObject.activeInHierarchy)
        {
            ShowStatus("Condition がありません。");
            Debug.LogWarning("❌ ExperimentStartButton: Condition が null または非表示です");
            return;
        }

        if (experimentController != null)
        {
            Debug.Log("✅ ExperimentStartButton: StartExperiment イベント送信中");
            experimentController.SendCustomEvent("StartExperiment");
            ShowStatus("実験を開始しました。");
        }
        else
        {
            Debug.LogError("❌ ExperimentStartButton: experimentController が設定されていません");
            ShowStatus("実験コントローラーが見つかりません");
        }
    }

    private void ShowStatus(string message)
    {
        if (statusTextUI != null)
        {
            statusTextUI.SetProgramVariable("statusText", message);
            statusTextUI.SendCustomEvent("ShowStatus");
        }
        else
        {
            Debug.Log($"ℹ️ 状態表示なし: {message}");
        }
    }
}
