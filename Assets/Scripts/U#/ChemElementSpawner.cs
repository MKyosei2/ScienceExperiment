using UdonSharp;
using UnityEngine;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemEnvironmentManager environmentManager;
    public int elementIndex = 0;  // ボタンから渡される
    public bool isCompound = false; // 互換フィールド（今回は未使用）

    // 3DボタンからもUIからも呼べる共通口
    public void Spawn()
    {
        if (environmentManager == null) { Debug.LogError("[ChemElementSpawner] EnvironmentManager 未設定"); return; }
        environmentManager.SpawnElement(elementIndex);
    }

    // 互換API
    public void StartExperiment() { Spawn(); }
    public void ResetExperiment() { if (environmentManager != null) environmentManager.ResetExperiment(); }

    public string SendMoleculeJson(string json = "")
    {
        // 将来のAI連携用：今は受け取りだけ
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
