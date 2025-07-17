using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class AIRequestSender : UdonSharpBehaviour
{
    public VRCUrl[] predefinedUrls;
    public Text statusText;
    public VRExperimentMonitor monitor;

    public void SendToAI(int urlIndex)
    {
        if (urlIndex < 0 || urlIndex >= predefinedUrls.Length)
        {
            if (statusText != null) statusText.text = "❌ URL範囲外";
            return;
        }

        string additionalParam = monitor != null ? monitor.GetLogText() : "";
        VRCUrl baseUrl = predefinedUrls[urlIndex];

        if (statusText != null) statusText.text = "📡 送信中...";

        // 追加情報は送れないが最低限送信できる状態にする
        VRCStringDownloader.LoadUrl(baseUrl, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        if (statusText != null) statusText.text = "✅ 応答成功";
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        if (statusText != null) statusText.text = "⚠️ 応答失敗";
    }
}
