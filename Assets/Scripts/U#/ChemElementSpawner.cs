using UdonSharp;
using UnityEngine;
using TMPro;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("参照")]
    public ChemEnvironmentManager environmentManager;
    public GameObject conicalFlaskPrefab;
    public GameObject formulaTextPrefab;
    public Transform spawnPoint;
    public Transform labelParent;

    [Header("元素情報")]
    public string[] elementSymbols; // 例: H, Li, Na
    public Color[] elementColors;   // 各元素の色
    private int currentIndex = -1;

    private GameObject currentFlask;
    private GameObject currentLabel;

    // =========================
    // 元素選択（ボタンから呼び出し）
    // =========================
    public void SelectElement(int index)
    {
        currentIndex = index;
        Debug.Log("[ChemElementSpawner] 元素選択: " + elementSymbols[index]);
        Spawn();
    }

    // =========================
    // フラスコ＋ラベル生成
    // =========================
    public void Spawn()
    {
        if (conicalFlaskPrefab == null)
        {
            Debug.LogError("[ChemElementSpawner] conicalFlaskPrefabが未設定");
            return;
        }
        if (formulaTextPrefab == null)
        {
            Debug.LogError("[ChemElementSpawner] formulaTextPrefabが未設定");
            return;
        }
        if (currentIndex < 0)
        {
            Debug.LogWarning("[ChemElementSpawner] 元素未選択");
            return;
        }

        // 古い生成物を削除
        if (currentFlask != null)
        {
            Destroy(currentFlask);
            currentFlask = null;
        }
        if (currentLabel != null)
        {
            Destroy(currentLabel);
            currentLabel = null;
        }

        string symbol = elementSymbols[currentIndex];
        Color color = Color.white;
        if (currentIndex < elementColors.Length)
        {
            color = elementColors[currentIndex];
        }

        // フラスコ生成
        currentFlask = VRCInstantiate(conicalFlaskPrefab);
        currentFlask.transform.position = spawnPoint.position;
        currentFlask.name = "CONICAL_FLASK_" + symbol;

        // 液体色変更
        if (environmentManager != null)
        {
            ChemVisualController visual = currentFlask.GetComponent<ChemVisualController>();
            if (visual != null)
            {
                visual.SetElementAppearance(color, ElementState.Liquid);
                visual.UpdateEnvironment(environmentManager.temperature, environmentManager.humidity, environmentManager.pressure);
            }
        }

        // ラベル生成
        currentLabel = VRCInstantiate(formulaTextPrefab);
        currentLabel.transform.SetParent(labelParent, false);
        currentLabel.name = "Label_" + symbol;

        TextMeshProUGUI text = currentLabel.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = symbol;
        }

        Debug.Log("[ChemElementSpawner] 生成完了: " + symbol);
    }

    // =========================
    // 実験開始
    // =========================
    public void StartExperiment()
    {
        if (environmentManager != null)
        {
            environmentManager.BeginReaction();
        }
        else
        {
            Debug.LogWarning("[ChemElementSpawner] EnvironmentManagerが未設定");
        }
    }

    // =========================
    // 実験リセット
    // =========================
    public void ResetExperiment()
    {
        if (environmentManager != null)
        {
            environmentManager.ResetEnvironment();
        }

        if (currentFlask != null)
        {
            Destroy(currentFlask);
            currentFlask = null;
        }
        if (currentLabel != null)
        {
            Destroy(currentLabel);
            currentLabel = null;
        }

        currentIndex = -1;
        Debug.Log("[ChemElementSpawner] 実験リセット完了");
    }

    // =========================
    // AI通信（デフォルト引数を追加！）
    // =========================
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

    // =========================
    // 化学結合制御
    // =========================
    public void ApplyBondUpdate(int atomIdA, int atomIdB, bool bonded)
    {
        if (environmentManager != null)
        {
            environmentManager.ApplyBondState(atomIdA, atomIdB, bonded);
        }
    }

    public void ApplyBondUpdate(int atomIdA, int atomIdB, int numericState)
    {
        bool bonded = numericState != 0;
        ApplyBondUpdate(atomIdA, atomIdB, bonded);
    }
}
