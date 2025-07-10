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
        if (text.Contains("🧬 結果"))
        {
            // パース（シンプルに処理）
            string result = Extract(text, "🧬 結果:", "\n");
            string trivia = Extract(text, "📖 雑学:", "\n");
            string style = Extract(text, "🎮 StyleID:", "\n");

            resultText.text = result;
            triviaText.text = trivia;

            int index = System.Array.IndexOf(styleIDs, style.Trim());
            if (index >= 0 && index < reactionPrefabs.Length)
            {
                Instantiate(reactionPrefabs[index], spawnPoint.position, Quaternion.identity);
            }

            File.Delete(filePath); // 一度読み込んだら削除
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
