using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Prefab")]
    public GameObject conicalFlaskPrefab;
    public Transform formulaTextParent;
    public GameObject formulaTextPrefab;

    [Header("元素データ")]
    public string[] elementKeys;
    public string[] elementFormulas;
    public Color[] elementColors;

    [Header("化合物データ")]
    public string[] compoundKeys;
    public string[] compoundFormulas;
    public Color[] compoundColors;

    private GameObject currentFlask;
    private ChemVisualController visualController;

    public void SpawnElement(int index, bool isCompound = false)
    {
        if (!isCompound && (index < 0 || index >= elementKeys.Length)) return;
        if (isCompound && (index < 0 || index >= compoundKeys.Length)) return;

        if (currentFlask != null) Destroy(currentFlask);

        currentFlask = VRCInstantiate(conicalFlaskPrefab);
        currentFlask.transform.position = transform.position;
        visualController = currentFlask.GetComponent<ChemVisualController>();

        string formula = "";
        Color color = Color.white;

        if (isCompound)
        {
            formula = compoundFormulas[index];
            color = compoundColors[index];
        }
        else
        {
            formula = elementFormulas[index];
            color = elementColors[index];
        }

        visualController.SetElementAppearance(color);

        GameObject textObj = VRCInstantiate(formulaTextPrefab);
        textObj.transform.SetParent(formulaTextParent, false);

        TMP_Text tmp = textObj.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = formula;
    }

    public void ResetExperiment()
    {
        if (currentFlask != null) Destroy(currentFlask);
        foreach (Transform child in formulaTextParent)
        {
            Destroy(child.gameObject);
        }
    }
}
