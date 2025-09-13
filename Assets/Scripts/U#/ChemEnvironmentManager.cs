// ChemEnvironmentManager.cs
// ベース環境（温度・湿度・圧力）を保持し、全フラスコへ一括反映するマネージャ。
// ・UdonSharp制約準拠（UnityEvent.AddListener/ActivateInputField 等は不使用）
// ・Slider と TMP_InputField を毎フレーム監視して双方向同期
// ・数値欄をユーザーがフォーカスすると VRCSDK によりキーボードが自動表示されます

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
using UnityEngine.UI;   // Slider
using TMPro;            // TMP_InputField
#if CHEM_RUNTIME
using UdonSharp;
#endif

#if CHEM_RUNTIME
public class ChemEnvironmentManager : UdonSharpBehaviour
#else
public class ChemEnvironmentManager : MonoBehaviour
#endif
{
    [Header("ベース環境（既定値）")]
    public float baseTempC = 20f;      // 温度[°C]
    public float baseHumidity = 50f;   // 湿度[%]
    public float basePressureAtm = 1f; // 圧力[atm]

    [Header("同時管理する最大フラスコ数")]
    public int maxControllers = 256;

    [Header("UI: スライダー（割り当て推奨）")]
    public Slider temperatureSlider;   // 例: Min=-273, Max=5000
    public Slider humiditySlider;      // 例: Min=0,    Max=200
    public Slider pressureSlider;      // 例: Min=0,    Max=100000

    [Header("UI: 数値入力（TMP_InputField）")]
    public TMP_InputField temperatureInput;
    public TMP_InputField humidityInput;
    public TMP_InputField pressureInput;

    [Header("同期設定")]
    public bool useUI = true;                 // UIを毎フレーム監視する
    public float changeThreshold = 0.0001f;   // この変化量を超えたときに反映
    public int displayDigits = 2;             // 表示小数桁

    // 内部：コントローラ管理（U#安全な固定配列）
    private ChemVisualController[] _controllers;
    private int _count = 0;

    // UI 監視用の前回値
    private float _prevTemp;
    private float _prevHum;
    private float _prevPress;

    private void Awake()
    {
        if (maxControllers <= 0) maxControllers = 64;
        _controllers = new ChemVisualController[maxControllers];
    }

    private void OnEnable()
    {
        // UI初期値→ベース値へ（割り当てがあれば優先）
        if (useUI)
        {
            if (temperatureSlider != null) baseTempC = temperatureSlider.value;
            if (humiditySlider != null) baseHumidity = humiditySlider.value;
            if (pressureSlider != null) basePressureAtm = pressureSlider.value;

            // 入力欄も初期表示
            SetInputText(temperatureInput, baseTempC);
            SetInputText(humidityInput, baseHumidity);
            SetInputText(pressureInput, basePressureAtm);
        }

        _prevTemp = baseTempC;
        _prevHum = baseHumidity;
        _prevPress = basePressureAtm;

        ApplyAll();
    }

    private void Update()
    {
        if (!useUI) return;

        bool changed = false;

        // --- 1) 入力欄が編集中なら、入力値を採用（→スライダーへも反映） ---
        if (temperatureInput != null && temperatureInput.isFocused)
        {
            float v;
            if (TryParseFloat(temperatureInput.text, out v))
            {
                if (Mathf.Abs(v - baseTempC) > changeThreshold)
                {
                    baseTempC = v;
                    if (temperatureSlider != null) temperatureSlider.value = v;
                    changed = true;
                }
            }
        }
        if (humidityInput != null && humidityInput.isFocused)
        {
            float v;
            if (TryParseFloat(humidityInput.text, out v))
            {
                if (Mathf.Abs(v - baseHumidity) > changeThreshold)
                {
                    baseHumidity = v;
                    if (humiditySlider != null) humiditySlider.value = v;
                    changed = true;
                }
            }
        }
        if (pressureInput != null && pressureInput.isFocused)
        {
            float v;
            if (TryParseFloat(pressureInput.text, out v))
            {
                if (v < 0f) v = 0f;
                if (Mathf.Abs(v - basePressureAtm) > changeThreshold)
                {
                    basePressureAtm = v;
                    if (pressureSlider != null) pressureSlider.value = v;
                    changed = true;
                }
            }
        }

        // --- 2) 入力欄が未フォーカスなら、スライダー→表示へ同期（スライダー未割当ならベース値で表示維持） ---
        if (temperatureSlider != null && !IsFocused(temperatureInput))
        {
            float sv = temperatureSlider.value;
            if (Mathf.Abs(sv - baseTempC) > changeThreshold)
            {
                baseTempC = sv;
                SetInputText(temperatureInput, baseTempC);
                changed = true;
            }
            else SyncInputIfMismatch(temperatureInput, baseTempC);
        }
        else if (!IsFocused(temperatureInput))
        {
            SyncInputIfMismatch(temperatureInput, baseTempC);
        }

        if (humiditySlider != null && !IsFocused(humidityInput))
        {
            float sv = humiditySlider.value;
            if (Mathf.Abs(sv - baseHumidity) > changeThreshold)
            {
                baseHumidity = sv;
                SetInputText(humidityInput, baseHumidity);
                changed = true;
            }
            else SyncInputIfMismatch(humidityInput, baseHumidity);
        }
        else if (!IsFocused(humidityInput))
        {
            SyncInputIfMismatch(humidityInput, baseHumidity);
        }

        if (pressureSlider != null && !IsFocused(pressureInput))
        {
            float sv = pressureSlider.value;
            if (sv < 0f) sv = 0f;
            if (Mathf.Abs(sv - basePressureAtm) > changeThreshold)
            {
                basePressureAtm = sv;
                SetInputText(pressureInput, basePressureAtm);
                changed = true;
            }
            else SyncInputIfMismatch(pressureInput, basePressureAtm);
        }
        else if (!IsFocused(pressureInput))
        {
            SyncInputIfMismatch(pressureInput, basePressureAtm);
        }

        // --- 3) 値が変わっていれば全反映 ---
        if (changed)
        {
            _prevTemp = baseTempC;
            _prevHum = baseHumidity;
            _prevPress = basePressureAtm;
            ApplyAll();
        }
    }

    // ===== 公開API（スクリプトやボタンから直接変更したい場合） =====
    public void SetTemperature(float celsius) { baseTempC = celsius; PushToUI(); ApplyAll(); }
    public void SetHumidity(float percent) { baseHumidity = percent; PushToUI(); ApplyAll(); }
    public void SetPressure(float atm) { basePressureAtm = (atm < 0f ? 0f : atm); PushToUI(); ApplyAll(); }

    // ===== コントローラ登録 / 解除（ChemVisualController から呼ばれる） =====
    public void Register(ChemVisualController c)
    {
        if (c == null) return;
        int idx = IndexOf(c);
        if (idx >= 0) return;
        if (_count < _controllers.Length)
        {
            _controllers[_count] = c;
            _count++;
        }
    }

    public void Unregister(ChemVisualController c)
    {
        if (c == null) return;
        int idx = IndexOf(c);
        if (idx < 0) return;
        int last = _count - 1;
        _controllers[idx] = _controllers[last];
        _controllers[last] = null;
        _count = last;
    }

    private int IndexOf(ChemVisualController c)
    {
        for (int i = 0; i < _count; i++) if (_controllers[i] == c) return i;
        return -1;
    }

    public void ApplyAll()
    {
        for (int i = 0; i < _count; i++)
        {
            ChemVisualController c = _controllers[i];
            if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                c.ApplyToShaders();
        }
    }

    // ===== ユーティリティ =====
    private bool TryParseFloat(string s, out float v)
    {
        if (string.IsNullOrEmpty(s)) { v = 0f; return false; }
        return float.TryParse(s, out v);
    }

    private void SetInputText(TMP_InputField input, float value)
    {
        if (input == null) return;
        input.text = ToFixed(value, displayDigits);
    }

    private void SyncInputIfMismatch(TMP_InputField input, float value)
    {
        if (input == null) return;
        string want = ToFixed(value, displayDigits);
        if (input.text != want) input.text = want;
    }

    private string ToFixed(float val, int digits)
    {
        if (digits <= 0) return Mathf.RoundToInt(val).ToString();
        float pow = Mathf.Pow(10f, digits);
        float r = Mathf.Round(val * pow) / pow;
        return r.ToString("F" + digits);
    }

    private bool IsFocused(TMP_InputField input)
    {
        return (input != null) && input.isFocused;
    }

    private void PushToUI()
    {
        if (temperatureSlider != null) temperatureSlider.value = baseTempC;
        if (humiditySlider != null) humiditySlider.value = baseHumidity;
        if (pressureSlider != null) pressureSlider.value = basePressureAtm;

        SyncInputIfMismatch(temperatureInput, baseTempC);
        SyncInputIfMismatch(humidityInput, baseHumidity);
        SyncInputIfMismatch(pressureInput, basePressureAtm);
    }
}
