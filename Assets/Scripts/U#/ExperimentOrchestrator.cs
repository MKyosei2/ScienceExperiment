// Assets/Scripts/U#/ExperimentOrchestrator.cs
using UdonSharp;
using UnityEngine;
using TMPro;

/// 開始→AIへリクエスト→（JSON/テキスト）再生 の司令塔。
public class ExperimentOrchestrator : UdonSharpBehaviour
{
    [Header("Wiring")]
    public SelectedObjectHolder selected;
    public AIRequestSender ai;

    [Header("Players (任意)")]
    public VisualExperimentPlayer visualPlayer;
    public JsonReactionPlayer jsonPlayer;

    [Header("UI (任意)")]
    public TextMeshProUGUI status;

    private bool isRunning = false;

    public void StartExperiment()
    {
        if (isRunning) return;

        if (selected == null || !selected.IsValid())
        {
            ShowStatus("⚠️ 選択が足りません（元素2以上、器具1以上、環境1）。");
            return;
        }

        isRunning = true;
        ShowStatus("⏳ 実験処理中... AIへ問い合わせ中");

        if (ai != null)
        {
            ai.RequestFromOrchestrator(this, selected.ToJsonPayload());
        }
        else
        {
            // AIなしの場合はテキスト再生のみ
            if (visualPlayer != null)
            {
                visualPlayer.PlayMessage("AI未接続のため、ダミー実験を再生します。");
            }
            isRunning = false;
            ShowStatus("✅ 完了（AI未接続）");
        }
    }

    // === AIRequestSender から呼ばれるコールバック ===
    public void OnAIText(string text)
    {
        if (visualPlayer != null) visualPlayer.PlayMessage(text);
        isRunning = false;
        ShowStatus("✅ 完了（テキスト応答）");
    }

    public void OnAIJson(string json)
    {
        if (jsonPlayer != null) jsonPlayer.PlayJson(json);
        else if (visualPlayer != null) visualPlayer.PlayMessage("JSON応答を受信: " + json);

        isRunning = false;
        ShowStatus("✅ 完了（JSON応答）");
    }

    private void ShowStatus(string s)
    {
        if (status != null) status.text = s;
    }
}
