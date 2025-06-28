using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ExperimentController : UdonSharpBehaviour
{
    [Header("依存コンポーネント")]
    public ElementSelector elementSelector;
    public ToolSelector toolSelector; // 🆕 実験器具選択
    public EnvironmentController environmentController;
    public AIRequestSender aiSender;
    public ExperimentValidator localValidator;

    [Tooltip("ローカル辞書のみで検証する場合 true")]
    public bool useLocalDictionary = false;

    public void RunExperiment()
    {
        // 元素チェック
        string symbol = elementSelector != null ? elementSelector.GetSelectedSymbol() : "";
        if (string.IsNullOrWhiteSpace(symbol))
        {
            Debug.Log("⚠️ 元素が選択されていません");
            return;
        }

        // 実験器具チェック
        string toolID = toolSelector != null ? toolSelector.GetSelectedToolID() : "None";

        // 実験条件の構築
        string[] elements = new string[] { symbol };
        string conditionKey = (environmentController != null
                               ? environmentController.GetConditionString()
                               : "Unknown") + "," + toolID;

        // ローカル or AI モード処理
        if (useLocalDictionary && localValidator != null)
        {
            localValidator.Validate(elements, conditionKey);
        }
        else if (aiSender != null)
        {
            aiSender.SendToAI(elements, conditionKey);
        }
    }
}