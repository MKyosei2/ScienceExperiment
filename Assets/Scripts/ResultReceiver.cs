using UnityEngine;
using TMPro;
using System.IO;

public class ResultReceiver : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI triviaText;
    public Transform spawnPoint;
    public GameObject[] reactionPrefabs;
    public string[] styleIDs;
    public ExperimentHistory history;
    public GameObject toolObject;
    public ShaderEffectData[] effectProfiles; // 追加: 実行時に適用するエフェクト

    private string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.streamingAssetsPath, "ExperimentResult.json");
        InvokeRepeating("CheckResult", 1f, 2f);
    }

    void CheckResult()
    {
        if (!File.Exists(filePath)) return;

        string text = File.ReadAllText(filePath);
        if (text.Contains("\uD83E\uDDEA 結果"))
        {
            string result = Extract(text, "\uD83E\uDDEA 結果:", "\n");
            string trivia = Extract(text, "\uD83D\uDCDA 雑学:", "\n");
            string style = Extract(text, "\uD83C\uDFAE StyleID:", "\n");

            if (resultText != null) resultText.text = result;
            if (triviaText != null) triviaText.text = trivia;

            int index = System.Array.IndexOf(styleIDs, style.Trim());
            if (index >= 0 && index < reactionPrefabs.Length)
            {
                Instantiate(reactionPrefabs[index], spawnPoint.position, Quaternion.identity);
            }

            ApplyEffectsToTool();

            if (history != null)
            {
                history.AddEntry("?", "?", "?", result, trivia);
            }

            File.Delete(filePath);
        }
    }

    void ApplyEffectsToTool()
    {
        if (toolObject == null || effectProfiles == null) return;

        var controller = toolObject.GetComponent<GlassRendererController>();
        if (controller != null)
        {
            controller.effects = effectProfiles;
            controller.ApplyEffects();
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
