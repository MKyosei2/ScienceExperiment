using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    public SelectionCategory category;
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;

    [Header("Element / Compound")]
    public int elementIndex;
    public bool isCompound;

    [Header("Equipment")]
    public int equipmentIndex;

    [Header("Condition")]
    public bool adjustTemperature; // true=温度, false=圧力
    public float step = 1f;

    public void Press()
    {
        if (category == SelectionCategory.Equipment)
        {
            if (environmentManager != null)
                environmentManager.SetEquipment(equipmentIndex);
            return;
        }

        if (category == SelectionCategory.Condition)
        {
            if (environmentManager != null)
            {
                if (adjustTemperature) environmentManager.AdjustTemperature(step);
                else environmentManager.AdjustPressure(step);
            }
            return;
        }

        // Element / Compound / Other → Spawnerへ
        if (elementSpawner != null)
        {
            elementSpawner.elementIndex = elementIndex;
            elementSpawner.isCompound = isCompound;
            elementSpawner.Spawn();
        }
    }
}
