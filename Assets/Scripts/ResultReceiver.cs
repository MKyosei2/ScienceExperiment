using UnityEngine;
using TMPro;
using System.IO;

public class ResultReceiver : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI triviaText;
    public GameObject[] reactionPrefabs;
    public string[] styleIDs;
    public ExperimentHistory history;
    public GameObject[] toolObjects;
    public GameObject[] elementObjects;
    public GameObject[] conditionObjects;
    public ShaderEffectData[] effectProfiles;

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
            string style = Extract(rawText, "🎮 StyleID:", "\n");

            bool fallback = false;

            string elementID = holder != null && holder.selectedElementIDs.Length > 0 ? holder.selectedElementIDs[0] : "?";
            string toolID = holder != null && holder.selectedToolIDs.Length > 0 ? holder.selectedToolIDs[0] : "?";
            string conditionID = holder != null ? holder.selectedConditionID : "?";

            if (string.IsNullOrEmpty(result))
            {
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

            int index = System.Array.IndexOf(styleIDs, style.Trim());
            if (index >= 0 && index < reactionPrefabs.Length)
            {
                Instantiate(reactionPrefabs[index], transform.position, Quaternion.identity);
            }
            else if (!fallback && reactionPrefabs.Length > 0)
            {
                Instantiate(reactionPrefabs[0], transform.position, Quaternion.identity);
            }

            ApplyEffectsToTargets(toolObjects);
            ApplyEffectsToTargets(elementObjects);
            ApplyEffectsToTargets(conditionObjects);

            if (history != null)
            {
                history.AddEntry(elementID, toolID, conditionID, result, trivia);
            }

            File.Delete(filePath);
        }
    }

    void ApplyEffectsToTargets(GameObject[] targets)
    {
        if (targets == null || effectProfiles == null) return;

        foreach (var obj in targets)
        {
            if (obj == null) continue;

            var controller = obj.GetComponent<GlassRendererController>();
            if (controller != null)
            {
                controller.effects = effectProfiles;
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