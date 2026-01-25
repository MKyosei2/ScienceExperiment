using UdonSharp;
using UnityEngine;
using TMPro;

/// <summary>
/// ChemEnvironmentManager
/// ・UIからの温度/湿度/圧力操作の受け口
/// ・各クライアントでUI表示（TextMeshPro）を更新
/// ・同期の真実は spawner 側に持たせる想定（ここは値の保持）
/// </summary>
public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Environment Values")]
    public float Temperature = 25f; // °C
    public float Humidity = 40f;    // %
    public float Pressure = 101f;   // kPa

    [Header("Defaults")]
    public float defaultTemperature = 25f;
    public float defaultHumidity = 40f;
    public float defaultPressure = 101f;

    [Header("UI Text (Optional)")]
    [Tooltip("温度の数値表示（例: +25.0 °C）")]
    public TMP_Text temperatureValueText;
    [Tooltip("湿度の数値表示（例: +40.0 %）")]
    public TMP_Text humidityValueText;
    [Tooltip("気圧の数値表示（例: +101.0 kPa）")]
    public TMP_Text pressureValueText;

    [Header("UI Formatting")]
    [Tooltip("正の数のときに '+' を付ける（例: +25.0）")]
    public bool showPlusSign = true;

    [Tooltip("数値の表示フォーマット（ToString）例: 0.0 / 0 / 0.00")]
    public string numberFormat = "0.0";

    [Header("Units (include leading space if you want)")]
    public string temperatureUnit = " °C";
    public string humidityUnit = " %";
    public string pressureUnit = " kPa";

    [Header("Optional: Debug/Status panel")]
    public ChemStatusDisplay statusDisplay;

    // alias (for other scripts)
    public float temperatureC { get { return Temperature; } }
    public float humidity { get { return Humidity; } }
    public float pressureKPa { get { return Pressure; } }

    private void Start()
    {
        ClampValues();
        RefreshUI();
    }

    // =============================================================
    // Modify（＋／− 調整の共通入口）
    // =============================================================
    public void Modify(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        // NOTE:
        // ボタン側の command は "TempUp" だけでなく "TEMP+" / "PRESS-" のような揺れがあるので吸収する。
        string c = command.Trim();

        // --- canonical first ---
        if (c == "TempUp") { Temperature += 1f; ClampValues(); RefreshUI(); return; }
        if (c == "TempDown") { Temperature -= 1f; ClampValues(); RefreshUI(); return; }

        if (c == "HumUp") { Humidity += 1f; ClampValues(); RefreshUI(); return; }
        if (c == "HumDown") { Humidity -= 1f; ClampValues(); RefreshUI(); return; }

        if (c == "PresUp") { Pressure += 1f; ClampValues(); RefreshUI(); return; }
        if (c == "PresDown") { Pressure -= 1f; ClampValues(); RefreshUI(); return; }

        // --- alternative spellings (ValueAdjustButton/ConditionAdjusterが使う) ---
        // TEMP+/TEMP- , HUMID+/HUMID- , PRESS+/PRESS-
        string u = c.ToUpper();
        bool plus = u.Contains("+") || u.EndsWith("UP");
        bool minus = u.Contains("-") || u.EndsWith("DOWN");

        // If neither is found, do nothing
        if (!plus && !minus) return;

        float delta = plus ? 1f : -1f;

        if (u.StartsWith("TEMP") || u.Contains("TEMP"))
        {
            Temperature += delta;
        }
        else if (u.StartsWith("HUMID") || u.StartsWith("HUM") || u.Contains("HUMID") || u.Contains("HUM"))
        {
            Humidity += delta;
        }
        else if (u.StartsWith("PRESS") || u.StartsWith("PRES") || u.Contains("PRESS") || u.Contains("PRES"))
        {
            Pressure += delta;
        }

        ClampValues();
        RefreshUI();
    }

    // =============================================================
    // UIボタンから直接呼べる（Udon用: 引数なし）
    // =============================================================
    public void _TempUp() { Temperature += 1f; ClampValues(); RefreshUI(); }
    public void _TempDown() { Temperature -= 1f; ClampValues(); RefreshUI(); }

    public void _HumUp() { Humidity += 1f; ClampValues(); RefreshUI(); }
    public void _HumDown() { Humidity -= 1f; ClampValues(); RefreshUI(); }

    public void _PresUp() { Pressure += 1f; ClampValues(); RefreshUI(); }
    public void _PresDown() { Pressure -= 1f; ClampValues(); RefreshUI(); }

    // =============================================================
    // 外部からまとめて値をセット（例: Spawnerから同期反映）
    // =============================================================
    public void SetValues(float tempC, float pressureKPaValue, float humidityPct)
    {
        Temperature = tempC;
        Pressure = pressureKPaValue;
        Humidity = humidityPct;
        ClampValues();
        RefreshUI();
    }

    public void _ResetToDefaults()
    {
        Temperature = defaultTemperature;
        Humidity = defaultHumidity;
        Pressure = defaultPressure;
        ClampValues();
        RefreshUI();
    }

    /// <summary>
    /// Environment UIとStatusDisplayをまとめて更新
    /// </summary>
    public void RefreshUI()
    {
        UpdateEnvironmentTexts();

        if (statusDisplay != null)
            statusDisplay.RefreshUI();
    }

    /// <summary>
    /// Inspectorに設定した Text を、現在の値で更新
    /// </summary>
    private void UpdateEnvironmentTexts()
    {
        if (temperatureValueText != null)
            temperatureValueText.text = FormatSignedValue(Temperature) + (temperatureUnit ?? "");

        if (humidityValueText != null)
            humidityValueText.text = FormatSignedValue(Humidity) + (humidityUnit ?? "");

        if (pressureValueText != null)
            pressureValueText.text = FormatSignedValue(Pressure) + (pressureUnit ?? "");
    }

    private string FormatSignedValue(float v)
    {
        string fmt = string.IsNullOrEmpty(numberFormat) ? "0.0" : numberFormat;
        string n = v.ToString(fmt);

        if (showPlusSign && v > 0f) return "+" + n;
        return n;
    }

    private void ClampValues()
    {
        Temperature = Mathf.Clamp(Temperature, -50f, 500f);
        Humidity = Mathf.Clamp(Humidity, 0f, 100f);
        Pressure = Mathf.Clamp(Pressure, 50f, 300f);
    }
}
