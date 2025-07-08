using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class AIRequestSender : UdonSharpBehaviour
{
    [Header("URL テンプレート設定（Unityで設定）")]
    [Tooltip("最大登録数 10 件程度まで推奨")]
    public VRCUrl[] predefinedUrls;

    public Text statusText;
    public AIReactionHandler handler;

    // 入力情報（Unity側の別スクリプトが呼び出す）
    public void SendToAI(int urlIndex)
    {
        if (urlIndex < 0 || urlIndex >= predefinedUrls.Length)
        {
            if (statusText != null) statusText.text = "❌ URLインデックス範囲外";
            return;
        }

        if (statusText != null) statusText.text = "AIに送信中...";
        VRCStringDownloader.LoadUrl(predefinedUrls[urlIndex], (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        if (handler != null) handler.HandleResponse(result.Result);
        if (statusText != null) statusText.text = "✅ AI応答成功";
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        if (statusText != null) statusText.text = "⚠️ AI応答エラー";
    }
}
