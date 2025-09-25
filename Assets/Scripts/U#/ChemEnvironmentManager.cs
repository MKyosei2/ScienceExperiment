using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("Prefab")]
    public GameObject[] equipmentPrefabs;   // フラスコ、ビーカー、試験管など
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

    [Header("環境パラメータ")]
    public float temperature;
    public float pressure;

    private GameObject currentEquipment;
    private ChemVisualController visualController;

    private int currentEquipmentIndex = 0; // デフォルトでフラスコ

    public void SetEquipment(int equipmentIndex)
    {
        currentEquipmentIndex = Mathf.Clamp(equipmentIndex, 0, equipmentPrefabs.Length - 1);
        Debug.Log($"[ChemEnvironmentManager] 器具を {equipmentPrefabs[currentEquipmentIndex].name} に切り替え");
    }

    public void SpawnElement(int index, bool isCompound = false)
    {
        if (!isCompound && (index < 0 || index >= elementKeys.Length)) return;
        if (isCompound && (index < 0 || index >= compoundKeys.Length)) return;

        if (currentEquipment != null) Destroy(currentEquipment);

        // 選択されている器具を生成
        currentEquipment = VRCInstantiate(equipmentPrefabs[currentEquipmentIndex]);
        currentEquipment.transform.position = transform.position;
        visualController = currentEquipment.GetComponent<ChemVisualController>();

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
        visualController.UpdateEnvironment(temperature, pressure);

        GameObject textObj = VRCInstantiate(formulaTextPrefab);
        textObj.transform.SetParent(formulaTextParent, false);

        TMP_Text tmp = textObj.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = formula;
    }

    public void ResetExperiment()
    {
        if (currentEquipment != null) Destroy(currentEquipment);
        foreach (Transform child in formulaTextParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void AdjustTemperature(float delta)
    {
        temperature += delta;
        if (visualController != null)
            visualController.UpdateEnvironment(temperature, pressure);
    }

    public void AdjustPressure(float delta)
    {
        pressure += delta;
        if (visualController != null)
            visualController.UpdateEnvironment(temperature, pressure);
    }
}
