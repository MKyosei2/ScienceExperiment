using UdonSharp;
using UnityEngine;
using TMPro;

public class AIRequestSender : UdonSharpBehaviour
{
    public VRExperimentMonitor monitor;
    public TextMeshProUGUI statusText;

    [Header("反応エフェクト用")]
    public Renderer reactionRenderer; // フラスコなどにアタッチされたMeshRenderer
    public Material reactionMaterial; // GlassMaster用マテリアルインスタンス

    public void SendToAI(string elementID, string toolID, string conditionID)
    {
        SendRequest(elementID, toolID, conditionID);
    }

    public void SendRequest(string elementID, string toolID, string conditionID)
    {
        string url = $"https://api.example.com/experiment?e={elementID}&t={toolID}&c={conditionID}";
        statusText.text = "🧪 リクエスト送信中...";
        if (monitor != null) monitor.Log("Request sent to: " + url);

        // 疑似レスポンス
        SendCustomEventDelayedSeconds(nameof(MockReceiveResponse), 2.0f);
    }

    public void MockReceiveResponse()
    {
        string result = "🔥 化学反応成功！酸素が放出されました。";
        statusText.text = result;
        if (monitor != null) monitor.Log(result);

        // 🔥 エフェクト発動
        TriggerReactionEffect();
    }

    public void TriggerReactionEffect()
    {
        if (reactionRenderer == null)
        {
            Debug.LogWarning("⚠️ reactionRenderer が未設定です");
            return;
        }

        Material mat = reactionRenderer.material; // Udon対応：ここで自動的にインスタンス化される

        if (mat == null)
        {
            Debug.LogWarning("⚠️ reactionRenderer にマテリアルが設定されていません");
            return;
        }

        // 🔥 シェーダーパラメータ調整
        mat.SetFloat("_BubbleSpeed", 3.0f);
        mat.SetFloat("_WobbleAmount", 0.3f);
        mat.SetFloat("_HeatDistortion", 0.3f);
        mat.SetColor("_MainColor", new Color(1f, 0.8f, 0.5f, 0.8f));
    }
}
