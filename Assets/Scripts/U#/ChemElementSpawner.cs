using UdonSharp;
using UnityEngine;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemEnvironmentManager environmentManager;
    public int elementIndex;
    public bool isCompound;

    public void Spawn()
    {
        environmentManager.SpawnElement(elementIndex, isCompound);
    }

    public void StartExperiment()
    {
        environmentManager.SpawnElement(elementIndex, isCompound);
    }

    public string SendMoleculeJson(string json = "")
    {
        Debug.Log($"[ChemElementSpawner] Molecule JSON received: {json}");
        return json;
    }

    public void ResetExperiment()
    {
        environmentManager.ResetExperiment();
    }

    public void ApplyBondUpdate(string updateJson)
    {
        Debug.Log($"[ChemElementSpawner] Bond update (JSON): {updateJson}");
    }

    public void ApplyBondUpdate(string atomA, string atomB, string bondType)
    {
        Debug.Log($"[ChemElementSpawner] Bond update: {atomA}-{atomB} ({bondType})");
    }

    public void ApplyBondUpdate(int atomA, int atomB, int bondType)
    {
        Debug.Log($"[ChemElementSpawner] Bond update (int): {atomA}-{atomB} ({bondType})");
    }
}
