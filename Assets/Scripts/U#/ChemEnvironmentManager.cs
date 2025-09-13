// ChemEnvironmentManager.cs
// ベース環境（温度/湿度/圧力）を保持し、全フラスコへ一括反映するマネージャ。
// UdonSharp制約対応：UnityEvent.AddListener等は使わず、UI Sliderを毎フレームポーリングして反映。
// 使い方：Canvas上の Slider を本スクリプトの temperatureSlider / humiditySlider / pressureSlider に割り当てるだけ。

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
using UnityEngine.UI;  // Slider参照
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

    [Header("UIスライダー（任意。割り当てれば自動ポーリング）")]
    public Slider temperatureSlider;   // 例：Min=-273, Max=5000
    public Slider humiditySlider;      // 例：Min=0,    Max=200
    public Slider pressureSlider;      // 例：Min=0,    Max=100000

    [Header("スライダー設定")]
    public bool useSliders = true;     // true: スライダーを毎フレーム読む
    public float changeThreshold = 0.0001f; // この値以上変わったら反映

    // 内部：固定長配列（U#安全）
    private ChemVisualController[] _controllers;
    private int _count = 0;

    // 前回値（スライダー監視用）
    private float _prevTemp;
    private float _prevHum;
    private float _prevPress;
    private bool _inited = false;

    private void Awake()
    {
        if (maxControllers <= 0) maxControllers = 64;
        _controllers = new ChemVisualController[maxControllers];
    }

    private void OnEnable()
    {
        // スライダーが割り当てられていれば、その現在値で初期化
        if (useSliders)
        {
            if (temperatureSlider != null) baseTempC = temperatureSlider.value;
            if (humiditySlider != null) baseHumidity = humiditySlider.value;
            if (pressureSlider != null) basePressureAtm = pressureSlider.value;
        }

        // 初期値をキャッシュ
        _prevTemp = baseTempC;
        _prevHum = baseHumidity;
        _prevPress = basePressureAtm;
        _inited = true;

        ApplyAll(); // 初期反映
    }

    private void Update()
    {
        if (!useSliders) return;

        bool changed = false;

        if (temperatureSlider != null)
        {
            float v = temperatureSlider.value;
            if (Mathf.Abs(v - baseTempC) > changeThreshold)
            {
                baseTempC = v;
                changed = true;
            }
        }

        if (humiditySlider != null)
        {
            float v = humiditySlider.value;
            if (Mathf.Abs(v - baseHumidity) > changeThreshold)
            {
                baseHumidity = v;
                changed = true;
            }
        }

        if (pressureSlider != null)
        {
            float v = pressureSlider.value;
            if (v < 0f) v = 0f; // 真空未満は0に
            if (Mathf.Abs(v - basePressureAtm) > changeThreshold)
            {
                basePressureAtm = v;
                changed = true;
            }
        }

        // 値が変わっていれば全フラスコへ反映
        if (changed)
        {
            _prevTemp = baseTempC;
            _prevHum = baseHumidity;
            _prevPress = basePressureAtm;
            ApplyAll();
        }
    }

    // ===== 登録 / 解除（ChemVisualController から呼ばれる） =====
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
        // 一杯なら無視（必要なら maxControllers を増やす）
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

    // ===== スクリプトから直接変更したい場合のAPI（ボタン併用も可） =====
    public void SetTemperature(float celsius) { baseTempC = celsius; ApplyAll(); }
    public void SetHumidity(float percent) { baseHumidity = percent; ApplyAll(); }
    public void SetPressure(float atm) { basePressureAtm = (atm < 0f ? 0f : atm); ApplyAll(); }

    // ===== 全反映 =====
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
}
