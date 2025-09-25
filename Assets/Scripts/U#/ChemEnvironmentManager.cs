using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("共通Prefab")]
    public GameObject conicalFlaskPrefab;

    [Header("UI用")]
    public Transform formulaTextParent;
    public GameObject formulaTextPrefab;   // GameObjectとして保持

    [Header("元素データ")]
    public string[] elementKeys;         // 元素ID (例: "H", "O", "Na")
    public string[] elementFormulas;     // 化学式 (例: "H₂", "O₂", "NaCl")
    public Color[] elementColors;        // Shaderに渡す色

    private GameObject currentFlask;
    private ChemVisualController visualController;

    public void SpawnElement(int index)
    {
        if (index < 0 || index >= elementKeys.Length) return;

        if (currentFlask != null) Destroy(currentFlask);

        // フラスコ生成
        currentFlask = VRCInstantiate(conicalFlaskPrefab);
        currentFlask.transform.position = transform.position;
        visualController = currentFlask.GetComponent<ChemVisualController>();

        // Shader変数反映
        visualController.SetElementAppearance(elementColors[index]);

        // 化学式テキスト生成
        GameObject textObj = VRCInstantiate(formulaTextPrefab);
        textObj.transform.SetParent(formulaTextParent, false);

        TMP_Text tmp = textObj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = elementFormulas[index];
        }
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
