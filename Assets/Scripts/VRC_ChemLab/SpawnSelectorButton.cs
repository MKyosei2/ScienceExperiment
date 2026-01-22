using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("Button Settings")]
    public SelectionCategory category = SelectionCategory.Element;

    // ボタン ID（元素記号・器具名・条件名）
    public string idOrName;

    [Header("Reference Targets")]
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;
    public ChemStatusDisplay statusDisplay;

    public override void Interact()
    {
        HandlePress();
    }

    public void HandlePress()
    {
        if (string.IsNullOrEmpty(idOrName))
        {
            Debug.LogWarning($"[SpawnSelectorButton] {name} の idOrName が設定されていません。");
            return;
        }


        // Auto-wire scene references if missing (scene inspector links can be lost)
        if (elementSpawner == null)
        {
            GameObject g = GameObject.Find("ChemElementSpawner");
            if (g != null) elementSpawner = g.GetComponent<ChemElementSpawner>();
        }
        if (environmentManager == null)
        {
            GameObject g2 = GameObject.Find("ChemEnvironmentManager");
            if (g2 != null) environmentManager = g2.GetComponent<ChemEnvironmentManager>();
        }
        if (statusDisplay == null)
        {
            GameObject g3 = GameObject.Find("ChemStatusDisplay");
            if (g3 != null) statusDisplay = g3.GetComponent<ChemStatusDisplay>();
        }

        if (elementSpawner == null)
        {
            Debug.LogError($"[SpawnSelectorButton] ChemElementSpawner が見つからないため処理できません: {name}");
            return;
        }

        switch (category)
        {
            case SelectionCategory.Element:
                if (elementSpawner != null)
                {
                    elementSpawner.SelectElement(idOrName);
                    Debug.Log($"Element Selected: {idOrName}");
                }
                break;

            case SelectionCategory.Tool:
                if (elementSpawner != null)
                {
                    elementSpawner.SelectEquipment(idOrName);
                    Debug.Log($"Equipment Selected: {idOrName}");
                }
                break;

            case SelectionCategory.Condition:
                // 条件は「同期の真実」を持つ Spawner 経由で更新する（視覚/温度モデルにも反映）
                if (elementSpawner != null)
                {
                    elementSpawner.ModifyEnvironment(idOrName);
                    Debug.Log($"Condition Modified (Spawner): {idOrName}");
                }
                else if (environmentManager != null)
                {
                    // フォールバック（Spawner未設定時のみ）
                    environmentManager.Modify(idOrName);
                    Debug.Log($"Condition Modified (EnvOnly): {idOrName}");
                }
                break;
        }

        if (statusDisplay != null)
            statusDisplay.RefreshUI();
    }
}
