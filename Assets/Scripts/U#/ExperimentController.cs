using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender aiRequestSender;
    public VRExperimentMonitor monitor;

    public void StartExperiment()
    {
        if (holder == null || aiRequestSender == null)
        {
            Debug.LogError("❌ holder または aiRequestSender が未設定");
            if (monitor) monitor.Log("❌ holder または aiRequestSender が未設定");
            return;
        }

        if (holder.selectedElementIDs == null || holder.selectedToolIDs == null || string.IsNullOrEmpty(holder.selectedConditionID))
        {
            Debug.LogWarning("⚠️ 実験に必要な選択が不足しています");
            if (monitor) monitor.Log("⚠️ 実験に必要な選択が不足しています");
            return;
        }

        // ログ出力（Discord用）★ここが追加ポイント
        string el = string.Join(", ", holder.selectedElementIDs);
        string tl = string.Join(", ", holder.selectedToolIDs);
        string cond = holder.selectedConditionID;
        Debug.Log("[EXPERIMENT_START] 元素: " + el + " | 器具: " + tl + " | 条件: " + cond);

        // モニタにも表示（任意）
        if (monitor)
        {
            monitor.Log("🧪 実験開始 → " + el + " + " + tl + " / 条件: " + cond);
        }

        // 実験をAIに送信
        aiRequestSender.SendToAI(holder.selectedElementIDs, holder.selectedToolIDs, holder.selectedConditionID);
    }
}
