
using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("References (recommended)")]
    [Tooltip("Assign the ChemElementSpawner directly if possible. If null, this script will try to find it by name.")]
    public ChemElementSpawner spawner;

    [Header("Name-based auto-find (Udon-safe)")]
    [Tooltip("If set, GameObject.Find will be used with this name to locate the spawner object.")]
    public string spawnerObjectName = "ChemElementSpawner";

    [Tooltip("Optional: additional candidate names to try if the primary name is not found.")]
    public string[] fallbackSpawnerNames = new string[] { "Spawner", "ElementSpawner", "VRC_ChemLab_Spawner" };

    [Tooltip("If assigned, used as the spawner object without searching.")]
    public GameObject spawnerObjectOverride;

    public void OnPress()
    {
        EnsureSpawner();
        if (spawner != null)
        {
            spawner.SpawnElement();
        }
        else
        {
            Debug.LogError("Spawner not assigned.");
        }
    }

    public override void Interact()
    {
        OnPress();
    }

    private void EnsureSpawner()
    {
        if (spawner != null) return;

        // 1) explicit override object
        if (spawnerObjectOverride != null)
        {
            spawner = spawnerObjectOverride.GetComponent<ChemElementSpawner>();
            if (spawner != null) return;
        }

        // 2) try by primary name
        if (!string.IsNullOrEmpty(spawnerObjectName))
        {
            GameObject go = GameObject.Find(spawnerObjectName);
            if (go != null)
            {
                spawner = go.GetComponent<ChemElementSpawner>();
                if (spawner != null) return;
            }
        }

        // 3) try fallback names
        if (fallbackSpawnerNames != null)
        {
            for (int i = 0; i < fallbackSpawnerNames.Length; i++)
            {
                string n = fallbackSpawnerNames[i];
                if (string.IsNullOrEmpty(n)) continue;
                GameObject go = GameObject.Find(n);
                if (go == null) continue;

                spawner = go.GetComponent<ChemElementSpawner>();
                if (spawner != null) return;
            }
        }

        // 4) last resort: walk up parents and see if any has the component (no reflection, Udon-safe)
        Transform t = transform;
        for (int i = 0; i < 12 && t != null; i++)
        {
            spawner = t.GetComponent<ChemElementSpawner>();
            if (spawner != null) return;
            t = t.parent;
        }
    }
}
