using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("環境パラメータ（デフォルト=現実世界）")]
    public float temperature = 20f;
    public float humidity = 0.5f;
    public float pressure = 1f;

    public void BeginReaction()
    {
        Debug.Log("[ChemEnvironmentManager] 実験開始処理");
    }

    public void ResetEnvironment()
    {
        temperature = 20f;
        humidity = 0.5f;
        pressure = 1f;
        Debug.Log("[ChemEnvironmentManager] 環境リセット完了");
    }

    public void AdjustTemperature(float delta)
    {
        temperature += delta;
        Debug.Log($"[ChemEnvironmentManager] 温度: {temperature}");
    }

    public void AdjustPressure(float delta)
    {
        pressure = Mathf.Max(0.1f, pressure + delta);
        Debug.Log($"[ChemEnvironmentManager] 圧力: {pressure}");
    }

    public void AdjustHumidity(float delta)
    {
        humidity = Mathf.Clamp01(humidity + delta);
        Debug.Log($"[ChemEnvironmentManager] 湿度: {humidity}");
    }

    public void SetEquipment(int index)
    {
        Debug.Log("[ChemEnvironmentManager] 器具切替 index=" + index);
    }

    // JSON送受信など
    public string ReceiveMoleculeJson(string json)
    {
        Debug.Log($"[ChemEnvironmentManager] JSON受信: {json}");
        return json;
    }

    public void ApplyBondState(int a, int b, bool state)
    {
        Debug.Log($"[ChemEnvironmentManager] Bond更新: {a}-{b} = {state}");
    }

    public void ApplyBondState(int a, int b, int numericState)
    {
        bool state = numericState != 0;
        ApplyBondState(a, b, state);
    }
}
