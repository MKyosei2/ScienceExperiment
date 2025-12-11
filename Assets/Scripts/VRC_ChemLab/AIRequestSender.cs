using UdonSharp;
using UnityEngine;

public class AIRequestSender : UdonSharpBehaviour
{
    public void RequestAI(string element, string tool, ChemElementSpawner spawner)
    {
        string compound = PredictCompound(element, tool);
        string explanation = GenerateExplanation(element, tool, compound);

        spawner.AppendAILog($"AI Prediction: {compound}\n{explanation}");
    }

    private string PredictCompound(string e, string t)
    {
        if (t.Contains("Burner")) return e + "O"; // 酸化
        if (t.Contains("Water")) return e + "H2O"; // 水和
        if (t.Contains("Acid")) return e + "Cl"; // 例：塩化

        return e + "?"; // 未知
    }

    private string GenerateExplanation(string e, string t, string c)
    {
        return $"Based on element '{e}' and tool '{t}', reaction likely forms '{c}'.";
    }
}
