using UdonSharp;
using UnityEngine;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemEnvironmentManager environmentManager;
    public int elementIndex;

    public void Spawn()
    {
        environmentManager.SpawnElement(elementIndex);
    }

    public void StartExperiment()
    {
        environmentManager.SpawnElement(elementIndex);
    }

    public string SendMoleculeJson()
    {
        return SendMoleculeJson("");
    }

    public string SendMoleculeJson(string json)
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
        Debug.Log($"[ChemElementSpawner] Bond update (1 arg): {updateJson}");
    }

    public void ApplyBondUpdate(string atomA, string atomB, string bondType)
    {
        Debug.Log($"[ChemElementSpawner] Bond update (3 args string): {atomA}-{atomB} ({bondType})");
    }

    public void ApplyBondUpdate(int atomA, int atomB, int bondType)
    {
        Debug.Log($"[ChemElementSpawner] Bond update (3 args int): {atomA}-{atomB} ({bondType})");
    }
}
