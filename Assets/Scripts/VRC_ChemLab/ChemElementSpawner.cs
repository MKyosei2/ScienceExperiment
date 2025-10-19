using UdonSharp;
using UnityEngine;

public class ChemElementSpawner : UdonSharpBehaviour
{
    public ChemEnvironmentManager environmentManager;
    public GameObject conicalFlaskPrefab;
    public Transform spawnPoint;

    public string[] elementSymbols;
    public Color[] elementColors;

    private int elementCount = 0;
    private string elemA = "";
    private string elemB = "";
    private bool equipmentSelected = false;
    private GameObject currentFlask;

    public void SelectElement(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return;

        if (elementCount == 0) elemA = symbol;
        else if (elementCount == 1) elemB = symbol;
        elementCount = Mathf.Min(2, elementCount + 1);

        if (currentFlask == null) SpawnFlask();
        ApplyElementColor(symbol);
        Debug.Log("[ChemElementSpawner] Element selected: " + symbol);
    }

    public void SelectEquipment()
    {
        equipmentSelected = true;
        Debug.Log("[ChemElementSpawner] Equipment selected");
    }

    public void StartExperiment()
    {
        if (currentFlask == null) SpawnFlask();

        var r = currentFlask ? currentFlask.GetComponentInChildren<Renderer>() : null;
        if (r && r.material)
        {
            float temp = environmentManager ? environmentManager.GetTemperature() : 20f;
            float hum = environmentManager ? environmentManager.GetHumidity() : 0.5f;

            if (r.material.HasProperty("_GlowIntensity"))
                r.material.SetFloat("_GlowIntensity", 2.0f);
            if (r.material.HasProperty("_BoilAmount"))
                r.material.SetFloat("_BoilAmount", Mathf.InverseLerp(20f, 100f, temp));
            if (r.material.HasProperty("_Humidity"))
                r.material.SetFloat("_Humidity", hum);
        }
    }

    public void ResetExperiment()
    {
        if (currentFlask) Destroy(currentFlask);
        currentFlask = null;
        elementCount = 0;
        elemA = elemB = "";
        equipmentSelected = false;
    }

    private void SpawnFlask()
    {
        if (!conicalFlaskPrefab || !spawnPoint)
        {
            Debug.LogError("[ChemElementSpawner] Prefab or SpawnPoint missing");
            return;
        }

        currentFlask = Instantiate(conicalFlaskPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint.parent);
        Debug.Log("[ChemElementSpawner] Flask spawned at: " + spawnPoint.position);
    }

    private void ApplyElementColor(string symbol)
    {
        if (!currentFlask || elementColors == null || elementSymbols == null) return;
        int idx = -1;
        for (int i = 0; i < elementSymbols.Length; i++)
        {
            if (elementSymbols[i] == symbol) { idx = i; break; }
        }
        if (idx < 0 || idx >= elementColors.Length) return;

        var r = currentFlask.GetComponentInChildren<Renderer>();
        if (r && r.material.HasProperty("_WireColor"))
            r.material.SetColor("_WireColor", elementColors[idx]);
    }

    public void ApplyBondUpdate(int atomIdA, int atomIdB, int bondedState)
    {
        bool bonded = bondedState != 0;
        if (environmentManager)
            environmentManager.ApplyBondState(atomIdA, atomIdB, bonded);
    }
}
