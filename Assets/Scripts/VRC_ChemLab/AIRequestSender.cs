using UdonSharp;
using UnityEngine;

public class AIRequestSender : UdonSharpBehaviour
{
    private ChemElementSpawner caller;

    public void RequestAI(string element, string tool, ChemElementSpawner spawner)
    {
        caller = spawner;

        if (caller == null)
        {
            Debug.LogError("[AI] Caller is null");
            return;
        }

        // ======== Step 1: ログ ========
        string log1 = $"AI: Analyzing reaction of {element} using {tool}...";
        caller.AppendAILog(log1);

        // ======== Step 2: 化合物生成の疑似ロジック ========
        string compound = PredictCompound(element, tool);

        // ======== Step 3: 説明文生成 ========
        string explanation = GenerateExplanation(element, tool, compound);

        // ======== Step 4: Spawner に渡す ========
        caller.AppendAILog($"AI: Predicted compound → {compound}");
        caller.AppendAILog(explanation);
    }

    private string PredictCompound(string element, string tool)
    {
        if (tool.Contains("FLASK"))
        {
            return element + "-solution";
        }
        if (tool.Contains("Gasburner"))
        {
            return element + "-oxide";
        }
        return element + "-compound";
    }

    private string GenerateExplanation(string element, string tool, string compound)
    {
        return $"AI: When {element} is processed with {tool}, it forms {compound}. " +
               $"This is a simplified prediction for educational simulation.";
    }
}
