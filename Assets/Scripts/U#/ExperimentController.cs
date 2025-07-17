using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;

    public void ExecuteExperiment()
    {
        if (holder == null || requestSender == null)
        {
            Debug.LogWarning("❌ 必要な参照が設定されていません (holder or requestSender)");
            return;
        }

        string elementID = holder.selectedElementID;
        string toolID = holder.selectedToolID;
        string conditionID = holder.selectedConditionID;

        if (string.IsNullOrEmpty(elementID) || string.IsNullOrEmpty(toolID) || string.IsNullOrEmpty(conditionID))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が完了していません");
            return;
        }

        Debug.Log($"🧪 実験開始：Element={elementID}, Tool={toolID}, Condition={conditionID}");
        requestSender.SendToAI(elementID, toolID, conditionID);
    }

    // 🔽 ExperimentStartButton から呼ばれる
    public void CollectFromTable()
    {
        Debug.Log("📥 実験台からオブジェクトを収集（仮）");
        // ここにオブジェクトのタグ or collider 検出処理を入れても良い
    }

    public void RunExperiment()
    {
        Debug.Log("▶ 実験を実行します");
        ExecuteExperiment(); // 内部でAI呼び出し
    }
}
