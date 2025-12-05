using UnityEngine;
using UdonSharp;
using TMPro;

public class ChemStatusDisplay : UdonSharpBehaviour
{
    [Header("UI (TextMeshPro)")]
    public TextMeshProUGUI statusText;

    [Header("References")]
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager environment;

    private string elementHistory = "";
    private string lastEquipment = "";
    private string lastAiLog = "";
    private string lastAiResult = "";

    private void Update()
    {
        if (statusText == null || spawner == null || environment == null)
            return;

        // 情報の更新
        elementHistory = spawner.GetElementHistory();
        lastEquipment = spawner.lastEquipmentName;

        // 温度・圧力・湿度は大文字の変数名に合わせる
        float temp = environment.Temperature;
        float press = environment.Pressure;
        float hum = environment.Humidity;

        // テキスト描画
        statusText.text =
            "<b><size=130%>Experiment Status</size></b>\n" +
            "------------------------------\n" +
            $"<b>Element:</b> {elementHistory}\n" +
            $"<b>Equipment:</b> {lastEquipment}\n\n" +
            $"<b>Environment</b>\n" +
            $" Temp: {temp} °C\n" +
            $" Pressure: {press} atm\n" +
            $" Humidity: {hum}%\n\n" +
            "<b>AI Log:</b>\n" +
            $"{lastAiLog}\n\n" +
            "<b>Result:</b>\n" +
            $"{lastAiResult}\n" +
            "------------------------------";
    }

    // --- 外部スクリプトからログを送るための関数 ---
    public void AddAiLog(string log)
    {
        lastAiLog = log;
    }

    public void SetAiResult(string result)
    {
        lastAiResult = result;
    }
}
