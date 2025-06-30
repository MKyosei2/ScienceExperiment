using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Components;
using VRC.SDK3.StringLoading;

public class PredictionFetcher : UdonSharpBehaviour
{
    public VRCUrl apiUrl;  // 例: https://yourrelay.example/api/last
    public CompoundPrefabAssembler assembler;
    public ResultDisplayManager displayManager;

    public void Fetch()
    {
        VRCStringDownloader.LoadUrl(apiUrl, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        string json = result.Result;
        string name = Extract(json, "\"compound\"");
        string styleStr = Extract(json, "\"style\"");
        int style = int.Parse(styleStr);

        displayManager.ShowResult(name + "（AIによる推論）");
        assembler.GenerateCompound(name, style);
    }

    private string Extract(string src, string key)
    {
        int s = src.IndexOf(key) + key.Length + 2;
        int e = src.IndexOf('"', s);
        return src.Substring(s, e - s);
    }
}
