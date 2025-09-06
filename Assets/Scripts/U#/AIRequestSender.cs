// Assets/Scripts/U#/AIRequestSender.cs
using UdonSharp;
using UnityEngine;

/// 実際のAI接続の代わりに、遅延してOrchestratorへ応答を返すモック。
public class AIRequestSender : UdonSharpBehaviour
{
    [Header("Mock Settings")]
    public bool returnJson = false;
    [TextArea] public string mockText = "混合後、白色沈殿が生成し、温度が上昇します。";
    [TextArea] public string mockJson = "{\"steps\":[{\"action\":\"mix\",\"intensity\":0.7},{\"action\":\"heat\",\"deltaT\":10}],\"effects\":[\"precipitate:white\"]}";
    public float responseDelay = 2.0f;

    private ExperimentOrchestrator _target;
    private string _payload;

    public void RequestFromOrchestrator(ExperimentOrchestrator target, string payloadJson)
    {
        _target = target;
        _payload = payloadJson;
        // ここで本来は外部AIに送る。今は遅延して自分に戻す。
        SendCustomEventDelayedSeconds(nameof(_DoRespond), responseDelay);
    }

    public void _DoRespond()
    {
        if (_target == null) return;

        if (returnJson)
        {
            _target.OnAIJson(mockJson);
        }
        else
        {
            // payload を使ってメッセージを変えるなども可能
            _target.OnAIText(mockText);
        }
        _target = null;
        _payload = null;
    }
}
