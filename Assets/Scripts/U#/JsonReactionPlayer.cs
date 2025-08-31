using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class JsonReactionPlayer : UdonSharpBehaviour
{
    [Header("Bindings")]
    public GlassRendererController glass;
    public TextAsset experimentJson;

    [Header("Audio/SE (Optional)")]
    public AudioSource seSource;
    public AudioClip seStart;
    public AudioClip seSuccess;
    public AudioClip seAbort;

    [Header("Execution")]
    public bool autoRunOnStart = false;

    // 状態機械（const は可）
    private const int ST_IDLE = 0;
    private const int ST_PREP = 1;
    private const int ST_RUN = 2;
    private const int ST_DONE = 3;
    private const int ST_ABORT = 4;
    private const int ST_CLEAN = 5;
    private int _state = ST_IDLE;

    // 実験メタ
    private string _mode = "canonical"; // or "hypothesis"
    private string _expId = "";
    private string _hash = "";
    private string _provenance = "";

    [TextArea(3, 10)]
    public string lastJson;

    void Start()
    {
        if (autoRunOnStart) PrepareAndRunFromTextAsset();
    }

    public void PrepareAndRunFromTextAsset()
    {
        if (experimentJson == null)
        {
            LogError("No experimentJson assigned.");
            return;
        }
        PrepareAndRun(experimentJson.text);
    }

    // Discord入口（IR済みJSONのみ）
    public void ReceiveDiscordJson(string jsonPayload)
    {
        PrepareAndRun(jsonPayload);
    }

    public void ForceCleanup()
    {
        TransitionTo(ST_CLEAN);
        DoCleanup();
        TransitionTo(ST_IDLE);
    }

    private void PrepareAndRun(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            LogError("Empty JSON.");
            return;
        }
        if (_state != ST_IDLE && _state != ST_DONE && _state != ST_ABORT)
        {
            LogWarn("Busy. Auto-clean and restart.");
            ForceCleanup();
        }

        lastJson = json;

        if (!TryParseAndValidate(json))
        {
            TransitionTo(ST_ABORT);
            PlaySE(seAbort);
            return;
        }

        if (_mode == "canonical")
        {
            if (!VerifyCanonicalHash(_hash, json))
            {
                LogError("Canonical hash verification failed.");
                TransitionTo(ST_ABORT);
                PlaySE(seAbort);
                return;
            }
        }

        TransitionTo(ST_PREP);
        if (glass != null) glass.CleanupVisual();
        ApplyVisualFromJson(json);
        PlaySE(seStart);

        TransitionTo(ST_RUN);
        // 時間発展は今後 "timeline" を拡張で

        TransitionTo(ST_DONE);
        PlaySE(seSuccess);
    }

    private void TransitionTo(int st) { _state = st; }

    private void DoCleanup()
    {
        if (seSource != null) seSource.Stop();
        if (glass != null) glass.CleanupVisual();
    }

    private bool TryParseAndValidate(string json)
    {
        _mode = ExtractEnum(json, "\"mode\"", new string[] { "canonical", "hypothesis" }, "canonical");
        _expId = ExtractString(json, "\"experiment_id\"", true);
        _hash = ExtractString(json, "\"hash\"", false);
        _provenance = ExtractString(json, "\"provenance\"", false);

        if (string.IsNullOrEmpty(_expId))
        {
            LogError("meta.experiment_id is required.");
            return false;
        }

        if (!BlockExists(json, "\"visual\""))
        {
            LogError("visual block missing.");
            return false;
        }

        // 安全項目（任意）
        if (BlockExists(json, "\"safety\""))
        {
            float tempK = ExtractFloat(json, "\"tempK\"", 298.15f);
            float press = ExtractFloat(json, "\"pressureKPa\"", 101.3f);
            int tox = ExtractInt(json, "\"tox\"", 0);
            if (tempK < 0f || tempK > 2000f) LogWarn("tempK out-of-bounds.");
            if (press < 0f || press > 10000f) LogWarn("pressureKPa out-of-bounds.");
            if (tox < 0 || tox > 3) LogWarn("tox should be 0..3.");
        }
        return true;
    }

    private void ApplyVisualFromJson(string json)
    {
        if (glass == null) return;

        float[] liq = ExtractFloatArray4(json, "\"liquid\"");
        float[] pre = ExtractFloatArray4(json, "\"precip\"");

        float viscosity = ExtractFloat(json, "\"viscosity\"", 0.5f);
        float wave = ExtractFloat(json, "\"wave\"", 0.0f);
        float foam = ExtractFloat(json, "\"foam\"", 0.0f);
        float heat = ExtractFloat(json, "\"heat\"", 0.0f);
        float turb = ExtractFloat(json, "\"turb\"", 0.0f);
        float fill = ExtractFloat(json, "\"fill\"", 0.4f);

        glass.SetLiquidRGBA(liq[0], liq[1], liq[2], liq[3]);
        glass.SetPrecipRGBA(pre[0], pre[1], pre[2], pre[3]);
        glass.SetPhysicals(viscosity, wave, foam, heat, turb, fill);

        // ResultReceiver との整合
        glass.ApplyEffects();
    }

    private bool VerifyCanonicalHash(string hash, string json)
    {
        if (string.IsNullOrEmpty(hash)) return true;
        // 実運用は外部計算ハッシュで検証
        return true;
    }

    // ====== 簡易 JSON util ======
    private bool BlockExists(string json, string key)
    {
        int idx = json.IndexOf(key);
        return idx >= 0;
    }
    private string ExtractEnum(string json, string key, string[] allowed, string defaultVal)
    {
        string s = ExtractString(json, key, false);
        if (string.IsNullOrEmpty(s)) return defaultVal;
        for (int i = 0; i < allowed.Length; i++) if (s == allowed[i]) return s;
        return defaultVal;
    }
    private string ExtractString(string json, string key, bool required)
    {
        int k = json.IndexOf(key);
        if (k < 0) { if (required) LogError("Missing key: " + key); return ""; }
        int colon = json.IndexOf(":", k); if (colon < 0) return "";
        int q1 = json.IndexOf("\"", colon + 1); if (q1 < 0) return "";
        int q2 = json.IndexOf("\"", q1 + 1); if (q2 < 0) return "";
        return json.Substring(q1 + 1, q2 - (q1 + 1));
    }
    private float ExtractFloat(string json, string key, float defVal)
    {
        int k = json.IndexOf(key);
        if (k < 0) return defVal;
        int colon = json.IndexOf(":", k); if (colon < 0) return defVal;
        int i = colon + 1; int end = i;
        for (; end < json.Length; end++)
        {
            char c = json[end];
            if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E') continue;
            break;
        }
        string num = json.Substring(i, end - i).Trim();
        float v; if (float.TryParse(num, out v)) return v;
        return defVal;
    }
    private int ExtractInt(string json, string key, int defVal)
    {
        float f = ExtractFloat(json, key, defVal);
        return Mathf.RoundToInt(f);
    }
    private float[] ExtractFloatArray4(string json, string key)
    {
        float[] arr = new float[4];
        int k = json.IndexOf(key);
        if (k < 0) { arr[0] = 0f; arr[1] = 0f; arr[2] = 0f; arr[3] = 0f; return arr; }
        int lb = json.IndexOf("[", k);
        int rb = json.IndexOf("]", lb + 1);
        if (lb < 0 || rb < 0 || rb <= lb) { arr[0] = 0; arr[1] = 0; arr[2] = 0; arr[3] = 0; return arr; }

        string inner = json.Substring(lb + 1, rb - (lb + 1));
        int count = 0; int start = 0;
        for (int i = 0; i < inner.Length && count < 4; i++)
        {
            if (inner[i] == ',')
            {
                string tok = inner.Substring(start, i - start).Trim();
                float v; if (!float.TryParse(tok, out v)) v = 0f;
                arr[count++] = v;
                start = i + 1;
            }
        }
        if (count < 4)
        {
            string last = inner.Substring(start).Trim();
            float v; if (!float.TryParse(last, out v)) v = 0f;
            arr[count++] = v;
        }
        while (count < 4) { arr[count++] = 0f; }
        return arr;
    }

    // ====== SE/Log ======
    private void PlaySE(AudioClip clip)
    {
        if (seSource == null || clip == null) return;
        seSource.Stop();
        seSource.clip = clip;
        seSource.Play();
    }
    private void LogError(string msg) { Debug.LogError("[JsonReactionPlayer] " + msg); }
    private void LogWarn(string msg) { Debug.LogWarning("[JsonReactionPlayer] " + msg); }
}