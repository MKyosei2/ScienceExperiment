using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    public float defaultTemperature = 20f;
    public float defaultHumidity = 0.5f;
    public float defaultPressure = 1f;

    public float temperature;
    public float humidity;
    public float pressure;

    private void Start() => ResetToDefaultsAndSync();

    public void AdjustTemperature(float delta) => temperature = Mathf.Clamp(temperature + delta, -273f, 2000f);
    public void AdjustHumidity(float delta) => humidity = Mathf.Clamp01(humidity + delta);
    public void AdjustPressure(float delta) => pressure = Mathf.Max(0f, pressure + delta);

    public void ResetToDefaultsAndSync()
    {
        temperature = defaultTemperature;
        humidity = defaultHumidity;
        pressure = defaultPressure;
    }

    public float GetTemperature() => temperature;
    public float GetHumidity() => humidity;
    public float GetPressure() => pressure;

    public void ApplyBondState(int atomIdA, int atomIdB, bool bonded) { }
}
