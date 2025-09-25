using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    public SelectionCategory category;   // Element / Compound / Equipment
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;
    public int elementIndex;
    public bool isCompound;
    public int equipmentIndex; // 器具選択用

    public void Press()
    {
        if (category == SelectionCategory.Equipment)
        {
            if (environmentManager != null)
                environmentManager.SetEquipment(equipmentIndex);
        }
        else
        {
            if (elementSpawner != null)
            {
                elementSpawner.elementIndex = elementIndex;
                elementSpawner.isCompound = isCompound;
                elementSpawner.Spawn();
            }
        }
    }
}
