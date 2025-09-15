#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
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
    public float basePressureAtm = 1f; // 圧力[atm]（0未満は0に）

    [Header("同時管理する最大フラスコ数")]
    public int maxControllers = 256;

    // 内部：固定長配列（U#安全）
    private ChemVisualController[] _controllers;
    private int _count = 0;

    private void Awake()
    {
        if (maxControllers <= 0) maxControllers = 64;
        _controllers = new ChemVisualController[maxControllers];
    }

    private void OnEnable()
    {
        // シーン起動時に一度反映
        ApplyAll();
    }

    // ===== コントローラ登録 / 解除（ChemVisualController から呼ばれる） =====
    public void Register(ChemVisualController c)
    {
        if (c == null) return;
        int idx = IndexOf(c);
        if (idx >= 0) return; // 既に登録済み

        if (_count < _controllers.Length)
        {
            _controllers[_count] = c;
            _count++;
        }
        // 超過時は無視（必要なら maxControllers を増やす）
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
        for (int i = 0; i < _count; i++)
        { 
            if (_controllers[i] == c) return i;
        }
        return -1;
    }

    // ====== UI（スライダー／入力欄）から直接呼ぶ公開メソッド ======
    // --- 温度 ---
    // Slider の OnValueChanged(float) から呼ぶ
    public void UI_SetTemperatureFloat(float celsius)
    {
        baseTempC = celsius;
        ApplyAll();
    }
    // TMP_InputField の OnEndEdit(string) / OnValueChanged(string) から呼ぶ
    public void UI_SetTemperatureString(string text)
    {
        float v;
        if (TryParseFloat(text, out v))
        {
            baseTempC = v;
            ApplyAll();
        }
    }

    // --- 湿度 ---
    public void UI_SetHumidityFloat(float percent)
    {
        baseHumidity = percent;
        ApplyAll();
    }
    public void UI_SetHumidityString(string text)
    {
        float v;
        if (TryParseFloat(text, out v))
        {
            baseHumidity = v;
            ApplyAll();
        }
    }

    // --- 圧力 ---
    public void UI_SetPressureFloat(float atm)
    {
        if (atm < 0f) atm = 0f;
        basePressureAtm = atm;
        ApplyAll();
    }
    public void UI_SetPressureString(string text)
    {
        float v;
        if (TryParseFloat(text, out v))
        {
            if (v < 0f) v = 0f;
            basePressureAtm = v;
            ApplyAll();
        }
    }

    // ===== スクリプトから直接セットしたい場合のAPI（任意） =====
    public void SetTemperature(float celsius) { baseTempC = celsius; ApplyAll(); }
    public void SetHumidity(float percent) { baseHumidity = percent; ApplyAll(); }
    public void SetPressure(float atm) { basePressureAtm = (atm < 0f ? 0f : atm); ApplyAll(); }

    // ===== 全フラスコへ反映 =====
    public void ApplyAll()
    {
        for (int i = 0; i < _count; i++)
        {
            ChemVisualController c = _controllers[i];
            if (c != null && c.enabled && c.gameObject.activeInHierarchy)
            {
                c.ApplyToShaders();
            }
        }
    }

    // ===== 文字列→float 変換（U#対応の簡易版） =====
    private bool TryParseFloat(string s, out float v)
    {
        if (string.IsNullOrEmpty(s)) { v = 0f; return false; }
        return float.TryParse(s, out v);
    }
}