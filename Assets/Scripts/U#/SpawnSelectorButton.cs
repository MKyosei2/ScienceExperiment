using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    public SelectionCategory category;     // ← string ではなく SelectionCategory 型
    public ChemElementSpawner elementSpawner;
    public int elementIndex;
    public bool isCompound;

    public void Press()
    {
        if (elementSpawner != null)
        {
            elementSpawner.elementIndex = elementIndex;
            elementSpawner.isCompound = isCompound;
            elementSpawner.Spawn();
        }
    }
}
