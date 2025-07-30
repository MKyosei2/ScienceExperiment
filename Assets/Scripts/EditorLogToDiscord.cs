using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class EditorLogToDiscord
{
    // 🧪 Creator's Lab サーバーの #ツールtoボット 用 Webhook URL を貼る
    const string WebhookUrl = "https://discord.com/api/webhooks/1399936246620618803/ETeqE97rcoaAo1hIbP_5eqtN6K3wXyzXoi2EPOlai7GT0xUciQndKEo-4goRpNqZzQ03"; // ← ここを正確に書き換える

    static async Task Main()
    {
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unity", "Editor", "Editor.log"
        );

        var seen = new HashSet<string>();
        Console.WriteLine("📡 Watching Unity Editor.log...");

        while (true)
        {
            try
            {
                string[] lines = File.ReadAllLines(logPath);
                foreach (var line in lines)
                {
                    if (line.Contains("[EXPERIMENT_START]") && !seen.Contains(line))
                    {
                        seen.Add(line);
                        string message = line.Substring(line.IndexOf("[EXPERIMENT_START]")).Trim();
                        await SendToDiscord(message);
                    }

                    if (line.Contains("[EXPERIMENT_REQUEST]") && !seen.Contains(line))
                    {
                        seen.Add(line);
                        int jsonStart = line.IndexOf('{');
                        string json = jsonStart >= 0 ? line.Substring(jsonStart).Trim() : line.Trim();
                        await SendToDiscord("📦 実験リクエスト JSON:\n```json\n" + json + "\n```");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("⚠️ Error reading log: " + e.Message);
            }

            await Task.Delay(2000);
        }
    }

    static async Task SendToDiscord(string message)
    {
        using (var client = new HttpClient())
        {
            var payload = new
            {
                content = message
            };

            string json = JsonConvert.SerializeObject(payload);
            var response = await client.PostAsync(
                WebhookUrl,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            Console.WriteLine($"✅ Discord送信: {response.StatusCode}");
        }
    }
}
