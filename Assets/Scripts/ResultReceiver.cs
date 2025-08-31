using UnityEngine;
using TMPro;
using System.IO;

public class ResultReceiver : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI triviaText;
    public GameObject[] toolObjects;

    public ShaderEffectData[] effectProfiles;
    public string[] styleIDs;

    public ExperimentHistory history;
    public SelectedObjectHolder holder;

    private string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.streamingAssetsPath, "ExperimentResult.json");
        InvokeRepeating("CheckResult", 1f, 2f);
    }

    void CheckResult()
    {
        if (!File.Exists(filePath)) return;

        string rawText = File.ReadAllText(filePath);

        if (rawText.Contains("🧪 結果"))
        {
            string result = Extract(rawText, "🧪 結果:", "\n");
            string trivia = Extract(rawText, "📖 雑学:", "\n");
            string style = Extract(rawText, "🎮 StyleID:", "\n").Trim();

            string elementID = holder != null && holder.selectedElementIDs.Length > 0 ? holder.selectedElementIDs[0] : "?";
            string toolID = holder != null && holder.selectedToolIDs.Length > 0 ? holder.selectedToolIDs[0] : "?";
            string conditionID = holder != null ? holder.selectedConditionID : "?";

            if (string.IsNullOrEmpty(result))
            {
                bool fallback = false;
                if (history != null)
                {
                    fallback = BotFallbackHelper.TryFallbackFromHistory(
                        history, elementID, toolID, conditionID,
                        out result, out trivia
                    );
                }

                if (!fallback)
                {
                    result = "❌ 記録が見つかりません";
                    trivia = "この組み合わせには履歴が存在しませんでした。";
                    Debug.LogWarning("❗ Bot応答も履歴も存在しませんでした");
                }
            }

            if (resultText != null) resultText.text = result;
            if (triviaText != null) triviaText.text = trivia;

            ApplyEffectToToolsByStyle(style);

            if (history != null)
            {
                history.AddEntry(elementID, toolID, conditionID, result, trivia);
            }

            File.Delete(filePath);
        }
    }

    void ApplyEffectToToolsByStyle(string styleID)
    {
        ShaderEffectData selectedEffect = null;
        for (int i = 0; i < styleIDs.Length && i < effectProfiles.Length; i++)
        {
            if (styleIDs[i] == styleID)
            {
                selectedEffect = effectProfiles[i];
                break;
            }
        }

        if (selectedEffect == null)
        {
            Debug.LogWarning($"⚠️ Style '{styleID}' に対応するエフェクトが見つかりません");
            return;
        }

        foreach (var obj in toolObjects)
        {
            if (obj == null) continue;
            var controller = obj.GetComponent<GlassRendererController>();
            if (controller != null)
            {
                controller.liquidColor = selectedEffect.liquidColor;
                controller.liquidAlpha = selectedEffect.liquidAlpha;
                controller.fillLevel = selectedEffect.fillLevel;
                controller.wobble = selectedEffect.wobble;

                controller.precipitateColor = selectedEffect.precipitateColor;
                controller.precipitateAmount = selectedEffect.precipitateAmount;

                controller.swirlStrength = selectedEffect.swirlStrength;
                controller.swirlSpeed = selectedEffect.swirlSpeed;

                controller.sparkle = selectedEffect.sparkle;
                controller.heat = selectedEffect.heat;

                controller.ApplyEffects();
            }
        }
    }

    string Extract(string source, string start, string end)
    {
        int i1 = source.IndexOf(start);
        if (i1 == -1) return "";
        i1 += start.Length;
        int i2 = source.IndexOf(end, i1);
        if (i2 == -1) i2 = source.Length;
        return source.Substring(i1, i2 - i1).Trim();
    }
}
