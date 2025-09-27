using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("必須プレハブ")]
    public GameObject conicalFlaskPrefab;
    public Transform formulaTextParent;
    public GameObject formulaTextPrefab;

    [Header("元素データ")]
    public string[] elementKeys;
    public string[] elementFormulas;
    public Color[] elementColors;

    [Header("環境パラメータ（デフォルト=現実世界）")]
    public float temperature = 20f;
    public float humidity = 0.5f;
    public float pressure = 1f;

    private GameObject currentFlask;
    private ChemVisualController currentVisual;

    public void SpawnElement(int index)
    {
        if (index < 0 || index >= elementKeys.Length) return;
        if (conicalFlaskPrefab == null) return;

        if (currentFlask != null) Destroy(currentFlask);

        currentFlask = VRCInstantiate(conicalFlaskPrefab);
        currentFlask.transform.position = transform.position;

        currentVisual = currentFlask.GetComponent<ChemVisualController>();
        if (currentVisual == null)
        {
            Debug.LogError("[ChemEnvironmentManager] Prefab に ChemVisualController が必要です");
            return;
        }

        ElementState state = DetermineState(elementKeys[index], temperature);
        var col = (index < elementColors.Length) ? elementColors[index] : Color.white;
        currentVisual.SetElementAppearance(col, state);
        currentVisual.UpdateEnvironment(temperature, humidity, pressure);
    }

    public void ResetExperiment()
    {
        if (currentFlask != null)
        {
            Destroy(currentFlask);
            currentFlask = null;
            currentVisual = null;
        }
    }

    public void AdjustTemperature(float delta)
    {
        temperature += delta;
        if (currentVisual != null) currentVisual.UpdateEnvironment(temperature, humidity, pressure);
    }

    public void AdjustHumidity(float delta)
    {
        humidity = Mathf.Clamp01(humidity + delta);
        if (currentVisual != null) currentVisual.UpdateEnvironment(temperature, humidity, pressure);
    }

    public void AdjustPressure(float delta)
    {
        pressure = Mathf.Max(0.1f, pressure + delta);
        if (currentVisual != null) currentVisual.UpdateEnvironment(temperature, humidity, pressure);
    }

    // --- 器具切替用（CategoryController 互換）---
    public void SetEquipment(int index)
    {
        Debug.Log("[ChemEnvironmentManager] 器具切替: index=" + index);
        // 今は ConicalFlask 固定。将来の拡張用
    }

    private ElementState DetermineState(string key, float temp)
    {
        key = key.ToLower();
        if (key == "h2o")
        {
            if (temp <= 0) return ElementState.Solid;
            if (temp >= 100) return ElementState.Gas;
            return ElementState.Liquid;
        }
        if (key == "o2" || key == "n2" || key == "co2") return ElementState.Gas;
        if (key == "nacl") return (temp >= 800) ? ElementState.Liquid : ElementState.Solid;
        if (key == "cuso4") return ElementState.Solid;
        return ElementState.Liquid;
    }
}
