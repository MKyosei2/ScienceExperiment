using UnityEngine;

/// <summary>
/// 外部/AIからの結果文字列を受け取り、内容に応じて出力先へ振り分ける。
/// - JSONっぽければ JsonReactionPlayer へ
/// - それ以外は VisualExperimentPlayer へ
/// - 空なら選択状況をログしてフォールバック演出
/// 旧実装の List.Length や BotFallbackHelper には依存しないように再実装。
/// </summary>
public class ResultReceiver : MonoBehaviour
{
    [SerializeField] private VRExperimentMonitor monitor;
    [SerializeField] private JsonReactionPlayer jsonPlayer;
    [SerializeField] private VisualExperimentPlayer visualPlayer;

    // 任意：フォールバック時に状況を出せるように
    [Header("Optional")]
    [SerializeField] private SelectedObjectHolder selected;

    /// <summary>
    /// 外部から結果を受け取るエントリポイント
    /// </summary>
    public void OnReceive(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            monitor?.LogWarning("ResultReceiver: empty message -> fallback.");
            PlayFallbackWithContext();
            return;
        }

        if (JsonHelper.IsJson(message))
        {
            monitor?.Log("ResultReceiver: JSON detected.");
            jsonPlayer?.Play(message);
        }
        else
        {
            monitor?.Log("ResultReceiver: Text detected.");
            visualPlayer?.PlayMessage(message);
        }
    }

    private void PlayFallbackWithContext()
    {
        if (selected)
        {
            // 選択状況をログに出すだけ（UIや演出は VisualExperimentPlayer に任せる）
            var summary = selected.ToSummaryString();
            monitor?.Log("Fallback with selection:\n" + summary);
        }
        visualPlayer?.PlayFallback();
    }
}
