using UdonSharp;
using UnityEngine;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemEnvironmentManager environmentManager;
    public int elementIndex;
    public bool isCompound;

    public void Spawn()
    {
        Debug.Log("[ChemElementSpawner] Spawn (Index=" + elementIndex + ", Compound=" + isCompound + ")");
    }

    public void StartExperiment()
    {
        Debug.Log("[ChemElementSpawner] StartExperiment");
        Spawn();
    }

    public void ResetExperiment()
    {
        Debug.Log("[ChemElementSpawner] ResetExperiment");
    }

    public string SendMoleculeJson(string json = "")
    {
        Debug.Log("[ChemElementSpawner] SendMoleculeJson: " + json);
        return json;
    }

    public void ApplyBondUpdate(string updateJson)
    {
        Debug.Log("[ChemElementSpawner] ApplyBondUpdate(JSON): " + updateJson);
    }

    public void ApplyBondUpdate(string atomA, string atomB, string bondType)
    {
        Debug.Log("[ChemElementSpawner] ApplyBondUpdate: " + atomA + "-" + atomB + " (" + bondType + ")");
    }

    public void ApplyBondUpdate(int atomA, int atomB, int bondType)
    {
        Debug.Log("[ChemElementSpawner] ApplyBondUpdate(int): " + atomA + "-" + atomB + " (" + bondType + ")");
    }
}
