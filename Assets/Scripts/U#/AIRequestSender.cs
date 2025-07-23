using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender : UdonSharpBehaviour
{
    [Header("UIとログ")]
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;

    [Header("反応対象Renderer")]
    public Renderer reactionRenderer;

    [Header("タイムアウト設定（秒）")]
    public float timeoutSeconds = 5.0f;

    private bool waitingResponse = false;
    private string elementID = "";
    private string toolID = "";
    private string conditionID = "";

    public void SendToAI(string e, string t, string c)
    {
        elementID = e;
        toolID = t;
        conditionID = c;

        waitingResponse = true;
        SendRequest(e, t, c);
        SendCustomEventDelayedSeconds(nameof(CheckForTimeout), timeoutSeconds);
    }

    public void SendRequest(string e, string t, string c)
    {
        string url = $"https://api.example.com/experiment?e={e}&t={t}&c={c}";
        statusText.text = "🧪 リクエスト送信中...";
        if (monitor != null) monitor.Log("Request sent to: " + url);

        // 疑似レスポンス（成功）
        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 2.0f);
    }

    public void MockReceiveResponse()
    {
        if (!waitingResponse) return;

        string result = $"🔥 成功: {elementID} + {toolID} + {conditionID}";
        statusText.text = result;
        if (monitor != null) monitor.Log(result);

        ApplyShaderEffectByCondition(elementID, toolID, conditionID);
        waitingResponse = false;
    }

    public void CheckForTimeout()
    {
        if (!waitingResponse) return;

        string fallback = "⚠️ 通信が失敗しました。仮の反応を表示します。";
        statusText.text = fallback;
        if (monitor != null) monitor.Log(fallback);

        ApplyVisualFallback();
        waitingResponse = false;
    }

    private void ApplyShaderEffectByCondition(string element, string tool, string condition)
    {
        if (reactionRenderer == null) return;
        Material mat = reactionRenderer.material;
        if (mat == null) return;

        // ✨ 条件ごとの演出を設定
        if (element.Contains("H") && element.Contains("O") && tool.Contains("burner"))
        {
            mat.SetFloat("_WobbleAmount", 0.12f);
            mat.SetFloat("_BubbleSpeed", 2.5f);
            mat.SetFloat("_HeatDistortion", 0.3f);
            mat.SetColor("_MainColor", new Color(0.2f, 0.6f, 1f, 1f)); // 水色
        }
        else if (element.Contains("Na") && element.Contains("Cl"))
        {
            mat.SetFloat("_WobbleAmount", 0.05f);
            mat.SetFloat("_BubbleSpeed", 0.8f);
            mat.SetFloat("_HeatDistortion", 0.05f);
            mat.SetColor("_MainColor", new Color(1f, 1f, 0.5f, 1f)); // 淡黄色
        }
        else
        {
            // その他（デフォルト）
            mat.SetFloat("_WobbleAmount", 0.08f);
            mat.SetFloat("_BubbleSpeed", 1.2f);
            mat.SetFloat("_HeatDistortion", 0.1f);
            mat.SetColor("_MainColor", new Color(0.8f, 0.8f, 1f, 1f)); // 薄青
        }
    }

    private void ApplyVisualFallback()
    {
        if (reactionRenderer == null) return;
        Material mat = reactionRenderer.material;
        if (mat == null) return;

        mat.SetFloat("_WobbleAmount", 0.02f);
        mat.SetFloat("_BubbleSpeed", 0.2f);
        mat.SetFloat("_HeatDistortion", 0.02f);
        mat.SetColor("_MainColor", new Color(1f, 0.3f, 0.3f, 1f)); // 赤
    }
}
