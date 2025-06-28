using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ExperimentController : UdonSharpBehaviour
{
    [Header("依存コンポーネント")]
    public ElementSelector elementSelector;
    public EnvironmentController environmentController;
    public AIRequestSender aiSender;
    public ExperimentValidator localValidator;

    [Tooltip("ローカル辞書のみで検証する場合 true")]
    public bool useLocalDictionary = false;

    public void RunExperiment()
    {
        // 元素選択チェック
        string symbol = elementSelector != null ? elementSelector.GetSelectedSymbol() : "";
        if (string.IsNullOrWhiteSpace(symbol))
        {
            Debug.Log("⚠️ 元素が選択されていません");
            return;
        }

        // 実験条件を組み立て
        string[] elements = new string[] { symbol };
        string envKey = environmentController != null
                              ? environmentController.GetConditionString()
                              : "Unknown";

        if (useLocalDictionary && localValidator != null)
        {
            // オフライン（AI を呼ばず辞書だけ）モード
            localValidator.Validate(elements, envKey);
        }
        else if (aiSender != null)
        {
            // AI サーバーへ送信
            aiSender.SendToAI(elements, envKey);
        }
    }
}
