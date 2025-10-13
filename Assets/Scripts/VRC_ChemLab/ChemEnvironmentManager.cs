using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("環境パラメータ（デフォルト値）")]
    public float temperature = 20f;
    public float humidity = 0.5f;
    public float pressure = 1f;

    [Header("UIリンク")]
    public TMP_InputField tempInput;
    public TMP_InputField humInput;
    public TMP_InputField presInput;

    [Header("実験制御")]
    public bool isReacting = false;
    public GameObject currentEquipment;

    private void Start()
    {
        UpdateUI();
    }

    // ============================================================
    // --- 基本環境制御 ---
    // ============================================================

    public void AdjustTemperature(float delta)
    {
        temperature = Mathf.Clamp(temperature + delta, -273f, 5000f);
        UpdateUI();
    }

    public void AdjustHumidity(float delta)
    {
        humidity = Mathf.Clamp(humidity + delta, 0f, 100f);
        UpdateUI();
    }

    public void AdjustPressure(float delta)
    {
        pressure = Mathf.Clamp(pressure + delta, 0f, 100f);
        UpdateUI();
    }

    // ============================================================
    // --- TMP入力欄変更時 ---
    // ============================================================

    public void OnTemperatureInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
            temperature = Mathf.Clamp(result, -273f, 5000f);
        UpdateUI();
    }

    public void OnHumidityInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
            humidity = Mathf.Clamp(result, 0f, 100f);
        UpdateUI();
    }

    public void OnPressureInputChanged(string value)
    {
        if (float.TryParse(value, out float result))
            pressure = Mathf.Clamp(result, 0f, 100f);
        UpdateUI();
    }

    // ============================================================
    // --- UI更新 ---
    // ============================================================

    private void UpdateUI()
    {
        if (tempInput != null)
            tempInput.text = temperature.ToString("0.##");
        if (humInput != null)
            humInput.text = humidity.ToString("0.##");
        if (presInput != null)
            presInput.text = pressure.ToString("0.###");
    }

    // ============================================================
    // --- 実験プロセス関連（Spawner・AI通信向けスタブ）---
    // ============================================================

    /// <summary>
    /// 実験反応を開始（Spawnerから呼ばれる）
    /// </summary>
    public void BeginReaction()
    {
        Debug.Log("[ChemEnvironmentManager] BeginReaction()");
        isReacting = true;
    }

    /// <summary>
    /// 実験終了・初期化
    /// </summary>
    public void ResetEnvironment()
    {
        Debug.Log("[ChemEnvironmentManager] ResetEnvironment()");
        isReacting = false;
        temperature = 20f;
        humidity = 0.5f;
        pressure = 1f;
        UpdateUI();
    }

    /// <summary>
    /// 外部（AIRequestSenderなど）から分子データを受け取り、文字列として返す
    /// </summary>
    public string ReceiveMoleculeJson(string json)
    {
        Debug.Log("[ChemEnvironmentManager] ReceiveMoleculeJson(): " + json);
        return json;
    }

    /// <summary>
    /// 結合状態を適用（Spawner側が (int, int, bool) で呼ぶ）
    /// </summary>
    public void ApplyBondState(int bondType, int bondState, bool autoUpdate)
    {
        Debug.Log("[ChemEnvironmentManager] ApplyBondState(): type=" + bondType + ", state=" + bondState + ", autoUpdate=" + autoUpdate);
        // TODO: Bond反映ロジック
    }

    /// <summary>
    /// 実験器具を設定（IDでもObjectでも対応可能）
    /// </summary>
    public void SetEquipment(GameObject equipment)
    {
        Debug.Log("[ChemEnvironmentManager] SetEquipment(GameObject): " + (equipment != null ? equipment.name : "null"));
        currentEquipment = equipment;
    }

    public void SetEquipment(int equipmentID)
    {
        Debug.Log("[ChemEnvironmentManager] SetEquipment(int): " + equipmentID);
        // 必要に応じて ID から GameObject をマッピング
    }
}
