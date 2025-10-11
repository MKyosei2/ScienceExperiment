using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("外部参照")]
    public ChemEnvironmentManager environmentManager;
    public GameObject conicalFlaskPrefab;     // 共通フラスコPrefab
    public GameObject formulaTextPrefab;      // 共通ラベルPrefab
    public Transform spawnPoint;              // フラスコ生成位置
    public Transform labelParent;             // ラベル生成位置

    [Header("元素情報")]
    public string[] elementSymbols;           // H, Li, Na ...
    public Color[] elementColors;             // 元素ごとの色
    private int currentElementIndex = -1;     // 現在選択中の元素

    private GameObject currentFlask;
    private GameObject currentLabel;

    // ==========================
    // 元素選択呼び出し
    // ==========================
    public void SelectElement(int index)
    {
        if (index < 0 || index >= elementSymbols.Length)
        {
            Debug.LogWarning("[ChemElementSpawner] 無効な元素ID: " + index);
            return;
        }

        currentElementIndex = index;
        Debug.Log($"[ChemElementSpawner] 元素選択: {elementSymbols[index]}");
    }

    // ==========================
    // 元素生成（ボタン押下時）
    // ==========================
    public void Spawn()
    {
        if (conicalFlaskPrefab == null || formulaTextPrefab == null)
        {
            Debug.LogError("[ChemElementSpawner] Prefabが設定されていません。");
            return;
        }

        if (currentElementIndex < 0)
        {
            Debug.LogWarning("[ChemElementSpawner] 元素が選択されていません。");
            return;
        }

        string symbol = elementSymbols[currentElementIndex];
        Color color = (currentElementIndex < elementColors.Length)
            ? elementColors[currentElementIndex]
            : Color.white;

        // 既存オブジェクト削除
        if (currentFlask != null) Destroy(currentFlask);
        if (currentLabel != null) Destroy(currentLabel);

        // フラスコ生成
        currentFlask = Instantiate(conicalFlaskPrefab, spawnPoint.position, Quaternion.identity);
        currentFlask.name = "CONICAL_FLASK_" + symbol;

        // 液体の色変更
        var visual = currentFlask.GetComponent<ChemVisualController>();
        if (visual != null)
        {
            visual.SetElementAppearance(color, ElementState.Liquid);
            visual.UpdateEnvironment(environmentManager.temperature, environmentManager.humidity, environmentManager.pressure);
        }

        // ラベル生成
        currentLabel = Instantiate(formulaTextPrefab, labelParent);
        currentLabel.name = "Label_" + symbol;

        var text = currentLabel.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) text = currentLabel.GetComponent<TextMeshProUGUI>();
        if (text != null) text.text = symbol;

        Debug.Log($"[ChemElementSpawner] 生成: {symbol} 色={color}");
    }

    // ==========================
    // 実験開始
    // ==========================
    public void StartExperiment()
    {
        if (environmentManager != null)
        {
            environmentManager.BeginReaction();
            Debug.Log("[ChemElementSpawner] 実験開始");
        }
    }

    // ==========================
    // 実験リセット
    // ==========================
    public void ResetExperiment()
    {
        if (environmentManager != null)
        {
            environmentManager.ResetEnvironment();
        }

        if (currentFlask != null) Destroy(currentFlask);
        if (currentLabel != null) Destroy(currentLabel);

        currentElementIndex = -1;

        Debug.Log("[ChemElementSpawner] 実験リセット完了");
    }

    // ==========================
    // AI通信・Bond更新
    // ==========================
    public string SendMoleculeJson(string json = "{}")
    {
        if (environmentManager != null)
        {
            string result = environmentManager.ReceiveMoleculeJson(json);
            Debug.Log("[ChemElementSpawner] JSON送信完了");
            return result;
        }
        return "{}";
    }

    public void ApplyBondUpdate(int atomIdA, int atomIdB, bool isBonded)
    {
        if (environmentManager != null)
            environmentManager.ApplyBondState(atomIdA, atomIdB, isBonded);
    }

    public void ApplyBondUpdate(int atomIdA, int atomIdB, int numericState)
    {
        bool bonded = numericState != 0;
        ApplyBondUpdate(atomIdA, atomIdB, bonded);
    }
}
