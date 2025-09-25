using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Prefabs")]
    public GameObject[] equipmentPrefabs;
    private int currentEquipmentIndex = 0;

    [Header("環境パラメータ")]
    public float temperature;
    public float pressure;

    public void SetEquipment(int equipmentIndex)
    {
        if (equipmentPrefabs.Length == 0) return;
        currentEquipmentIndex = Mathf.Clamp(equipmentIndex, 0, equipmentPrefabs.Length - 1);
        Debug.Log("[ChemEnvironmentManager] SetEquipment: " + equipmentPrefabs[currentEquipmentIndex].name);
    }

    public GameObject GetCurrentEquipment()
    {
        if (equipmentPrefabs.Length == 0) return null;
        return equipmentPrefabs[currentEquipmentIndex];
    }

    public void AdjustTemperature(float delta)
    {
        temperature += delta;
        Debug.Log("[ChemEnvironmentManager] Temperature = " + temperature);
    }

    public void AdjustPressure(float delta)
    {
        pressure += delta;
        Debug.Log("[ChemEnvironmentManager] Pressure = " + pressure);
    }
}
