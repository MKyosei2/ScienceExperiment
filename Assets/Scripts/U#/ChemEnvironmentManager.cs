using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("環境パラメータ（デフォルト値）")]
    public float temperature = 20f;
    public float humidity = 0.5f;
    public float pressure = 1f;

    // --------------------------
    // 器具切替
    // --------------------------
    public void SetEquipment(int index)
    {
        Debug.Log("[ChemEnvironmentManager] 器具切替 index=" + index);
    }

    // --------------------------
    // 環境調整
    // --------------------------
    public void AdjustTemperature(float delta)
    {
        temperature += delta;
        Debug.Log("[ChemEnvironmentManager] 温度変更: " + temperature);
    }

    public void AdjustPressure(float delta)
    {
        pressure = Mathf.Max(0.1f, pressure + delta);
        Debug.Log("[ChemEnvironmentManager] 圧力変更: " + pressure);
    }

    public void AdjustHumidity(float delta)
    {
        humidity = Mathf.Clamp01(humidity + delta);
        Debug.Log("[ChemEnvironmentManager] 湿度変更: " + humidity);
    }

    // --------------------------
    // 実験制御
    // --------------------------
    public void BeginReaction()
    {
        Debug.Log("[ChemEnvironmentManager] 実験開始");
    }

    public void ResetEnvironment()
    {
        temperature = 20f;
        humidity = 0.5f;
        pressure = 1f;
        Debug.Log("[ChemEnvironmentManager] 環境リセット完了");
    }

    // --------------------------
    // AI通信と結合制御
    // --------------------------
    public string ReceiveMoleculeJson(string json)
    {
        Debug.Log("[ChemEnvironmentManager] JSON受信: " + json);
        return json;
    }

    public void ApplyBondState(int atomIdA, int atomIdB, bool bonded)
    {
        Debug.Log("[ChemEnvironmentManager] 結合更新: " + atomIdA + "-" + atomIdB + " = " + bonded);
    }

    public void ApplyBondState(int atomIdA, int atomIdB, int numericState)
    {
        bool bonded = numericState != 0;
        ApplyBondState(atomIdA, atomIdB, bonded);
    }
}
