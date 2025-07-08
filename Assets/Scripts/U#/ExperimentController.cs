using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public ExperimentSelector selector;
    public AIRequestSender requestSender;

    public void RunExperiment()
    {
        if (selector == null || requestSender == null) return;

        string symbol = selector.GetSymbol();
        string toolID = selector.GetToolID();
        string conditionID = selector.GetConditionID();

        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(toolID) || string.IsNullOrWhiteSpace(conditionID))
        {
            Debug.Log("⚠️ 条件が未設定です");
            return;
        }

        // 条件に応じてURLのインデックスを算出（例：0固定、または symbol + toolID + conditionID に応じて割り振るなど）
        int urlIndex = 0; // TODO: 条件に応じて切り替えたい場合はここをロジックにする

        requestSender.SendToAI(urlIndex);
    }
}
