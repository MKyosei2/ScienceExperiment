using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    [Header("必須プレハブ（統一してここだけに割当）")]
    public GameObject conicalFlaskPrefab;       // ← 必ず CONICAL_FLASK を割当
    public Transform formulaTextParent;         // 空の親（ラベル表示位置）
    public GameObject formulaTextPrefab;        // TMP テキストのプレハブ

    [Header("元素データ")]
    public string[] elementKeys;                // 例: H, O, Na...
    public string[] elementFormulas;            // 例: H₂, O₂, Na, ...
    public Color[] elementColors;               // 実在色 or 指定色

    [Header("環境パラメータ")]
    public float temperature = 20f;
    public float pressure = 1f;

    private GameObject currentFlask;
    private ChemVisualController currentVisual;

    public void SpawnElement(int index)
    {
        if (index < 0 || index >= elementKeys.Length) return;
        if (conicalFlaskPrefab == null)
        {
            Debug.LogError("[ChemEnvironmentManager] conicalFlaskPrefab 未設定");
            return;
        }

        // 既存を破棄
        if (currentFlask != null) Destroy(currentFlask);

        // 常に CONICAL_FLASK だけを生成
        currentFlask = VRCInstantiate(conicalFlaskPrefab);
        currentFlask.transform.position = transform.position;

        // Prefab に必ず ChemVisualController をアタッチしておくこと！
        currentVisual = currentFlask.GetComponent<ChemVisualController>();
        if (currentVisual == null)
        {
            Debug.LogError("[ChemEnvironmentManager] Prefab に ChemVisualController が付いていません！");
            return;
        }

        // 見た目・環境反映
        var col = (index < elementColors.Length) ? elementColors[index] : Color.white;
        currentVisual.SetElementAppearance(col);
        currentVisual.UpdateEnvironment(temperature, pressure);

        // 化学式を表示
        if (formulaTextPrefab != null && formulaTextParent != null)
        {
            // 既存の式を全消し
            for (int i = formulaTextParent.childCount - 1; i >= 0; i--)
                Destroy(formulaTextParent.GetChild(i).gameObject);

            var textObj = VRCInstantiate(formulaTextPrefab);
            textObj.transform.SetParent(formulaTextParent, false);

            var tmp = textObj.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                string formula = (index < elementFormulas.Length) ? elementFormulas[index] : elementKeys[index];
                tmp.text = formula;
            }
        }
    }

    public void ResetExperiment()
    {
        if (currentFlask != null)
        {
            Destroy(currentFlask);
            currentFlask = null;
            currentVisual = null;
        }
        if (formulaTextParent != null)
        {
            for (int i = formulaTextParent.childCount - 1; i >= 0; i--)
                Destroy(formulaTextParent.GetChild(i).gameObject);
        }
    }

    public void AdjustTemperature(float delta)
    {
        temperature += delta;
        if (currentVisual != null) currentVisual.UpdateEnvironment(temperature, pressure);
    }

    public void AdjustPressure(float delta)
    {
        pressure += delta;
        if (currentVisual != null) currentVisual.UpdateEnvironment(temperature, pressure);
    }
}
