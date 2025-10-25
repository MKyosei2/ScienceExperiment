using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections.Generic;

[AddComponentMenu("VRC Lab/ChemElementSpawner")]
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("Spawn Roots")]
    public Transform spawnParent;                     // 生成の親 (例: Systems/Spawner)

    [Header("Materials")]
    public Material wireMaterial;                     // 器具に適用するワイヤーフレーム用
    public Material elementVisualMaterial;            // 元素見た目用（共通Shader。_Color を使う想定）

    [Header("Common Prefabs")]
    [Tooltip("元素ボタンを押した際に使う共通器具（例: CONICAL_FLASK）")]
    public GameObject defaultElementVesselPrefab;     // 元素プレビュー用・共通器具

    [Tooltip("実験器具ボタンから選ばれる実験器具の候補（自由に拡張可能）")]
    public GameObject[] equipmentPrefabs;             // 実験用器具はここから名称一致で選ぶ

    [Header("Element Visual Hook")]
    [Tooltip("共通器具Prefabの子階層にある、元素の見た目を描画する子の名前（Renderer を持つ）。例: \"ElementVisual\"")]
    public string elementVisualChildName = "ElementVisual";

    [Header("Reaction System")]
    public JsonReactionPlayer reactionPlayer;

    // ==== 外部連携（他スクリプト互換） ====
    [HideInInspector] public string selectedEquipmentName = "";  // Tool ボタンでセット
    [HideInInspector] public string selectedElementName = "";  // Element ボタンでセット
    [HideInInspector] public string bondData = "";               // AI 応答

    // 内部管理
    private GameObject currentElementPreview;                    // 元素ボタンで生成された共通器具（プレビュー）
    private GameObject[] spawnedExperimentObjects = new GameObject[32];
    private int spawnedCount = 0;

    // ===================== 旧互換メソッド（他スクリプトからの呼び出し互換） =====================
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectEquipment(string n) { selectedEquipmentName = n; SendCustomEvent("_SelectEquipment"); }
    public void SelectElement(string n) { selectedElementName = n; SendCustomEvent("_SelectElement"); }
    public void SpawnSelectedVesselAndStart() { SendCustomEvent("_StartExperiment"); }

    // ===================== 選択・生成ロジック =====================

    /// <summary>
    /// Tool ボタンで器具名を選んだとき：名称だけ保持（生成は StartExperiment で行う）
    /// </summary>
    public void _SelectEquipment()
    {
        if (string.IsNullOrEmpty(selectedEquipmentName))
        {
            Debug.LogWarning("[Spawner] 器具名が空です。");
            return;
        }

        var prefab = FindPrefabByName(equipmentPrefabs, selectedEquipmentName);
        if (prefab != null)
        {
            Debug.Log($"[Spawner] 器具 '{selectedEquipmentName}' を選択（実生成は開始時）。");
        }
        else
        {
            Debug.LogWarning($"[Spawner] 器具 '{selectedEquipmentName}' が equipmentPrefabs に見つかりません。");
        }
    }

    /// <summary>
    /// Element ボタンで元素名を選んだとき：共通器具をその場で1つ生成し、Shader パラメータで元素見た目を表現
    /// </summary>
    public void _SelectElement()
    {
        if (string.IsNullOrEmpty(selectedElementName))
        {
            Debug.LogWarning("[Spawner] 元素名が空です。");
            return;
        }

        // 既存のプレビューを消して作り直す
        if (currentElementPreview != null)
        {
            Destroy(currentElementPreview);
            currentElementPreview = null;
        }

        if (defaultElementVesselPrefab == null)
        {
            Debug.LogError("[Spawner] defaultElementVesselPrefab が未設定です。");
            return;
        }

        // 共通器具（例えば CONICAL_FLASK）を生成
        currentElementPreview = VRCInstantiate(defaultElementVesselPrefab);
        if (spawnParent != null)
        {
            currentElementPreview.transform.SetParent(spawnParent);
            currentElementPreview.transform.localPosition = Vector3.zero;
            currentElementPreview.transform.localRotation = Quaternion.identity;
            currentElementPreview.transform.localScale = Vector3.one;
        }

        // 器具本体にワイヤーフレーム適用
        ApplyWireframe(currentElementPreview);

        // 元素見た目（共通Shader）を適用：指定名の子を探して色などを設定
        ApplyElementVisual(currentElementPreview, selectedElementName);

        Debug.Log($"[Spawner] 元素 '{selectedElementName}' を選択 → 共通器具でプレビュー生成。");
    }

    /// <summary>
    /// 実験開始：
    /// - 器具ボタンで選ばれた器具を生成（未選択なら defaultElementVesselPrefab を使用）
    /// - 実験時は器具のみ生成（元素表現は Shader に委譲）
    /// </summary>
    public void _StartExperiment()
    {
        var vesselPrefab = FindPrefabByName(equipmentPrefabs, selectedEquipmentName);
        if (vesselPrefab == null)
        {
            // 未選択なら共通器具を使う
            vesselPrefab = defaultElementVesselPrefab;
            if (vesselPrefab == null)
            {
                Debug.LogError("[Spawner] 実験開始できません：器具が未選択で、defaultElementVesselPrefab も未設定です。");
                return;
            }
            if (!string.IsNullOrEmpty(selectedEquipmentName))
                Debug.LogWarning($"[Spawner] 器具 '{selectedEquipmentName}' が見つからないため、共通器具で代用します。");
        }

        // 実験用の器具を生成
        GameObject vessel = VRCInstantiate(vesselPrefab);
        if (spawnParent != null)
        {
            vessel.transform.SetParent(spawnParent);
            vessel.transform.localPosition = Vector3.zero;
            vessel.transform.localRotation = Quaternion.identity;
            vessel.transform.localScale = Vector3.one;
        }
        TrackSpawn(vessel);

        // 実験中は「器具生成物」で反応を行う → 器具にワイヤー適用（見やすさ向上）
        ApplyWireframe(vessel);

        Debug.Log($"[Spawner] 実験開始：器具 = {(vesselPrefab != null ? vesselPrefab.name : "null")}, 元素 = {selectedElementName}");

        // プレビューは役目終了なので片付け（見せたいなら残しても良い）
        if (currentElementPreview != null)
        {
            Destroy(currentElementPreview);
            currentElementPreview = null;
        }
    }

    /// <summary>
    /// 実験リセット：生成物はすべて削除、環境側は Orchestrator がリセット
    /// </summary>
    public void _ResetExperiment()
    {
        // 実験用生成物を破棄
        for (int i = 0; i < spawnedCount; i++)
        {
            if (spawnedExperimentObjects[i] != null)
                Destroy(spawnedExperimentObjects[i]);
        }
        spawnedCount = 0;

        // プレビューも破棄
        if (currentElementPreview != null)
        {
            Destroy(currentElementPreview);
            currentElementPreview = null;
        }

        // 選択状態は維持しても良いが、誤操作防止でクリア
        // （必要ならコメントアウト）
        selectedElementName = "";
        selectedEquipmentName = "";

        Debug.Log("[Spawner] 実験リセット完了（生成物・プレビューを全削除）。");
    }

    /// <summary>
    /// AI応答（化学結合等）を適用：JsonReactionPlayerへ転送
    /// </summary>
    public void _ApplyBondUpdate()
    {
        if (string.IsNullOrEmpty(bondData))
        {
            Debug.LogWarning("[Spawner] bondData が空のため適用をスキップ。");
            return;
        }

        Debug.Log($"[Spawner] AI反応を適用: {bondData}");
        if (reactionPlayer != null)
            reactionPlayer.Play(bondData);
    }

    // ===================== 内部補助 =====================

    private GameObject FindPrefabByName(GameObject[] list, string name)
    {
        if (list == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < list.Length; i++)
        {
            var g = list[i];
            if (g != null && g.name == name) return g;
        }
        return null;
    }

    private void TrackSpawn(GameObject obj)
    {
        if (obj == null) return;
        if (spawnedCount >= spawnedExperimentObjects.Length) return;
        spawnedExperimentObjects[spawnedCount++] = obj;
    }

    private void ApplyWireframe(GameObject root)
    {
        if (wireMaterial == null || root == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            // 子の ElementVisual にまでワイヤーを当てたくない場合はフィルタ可能
            renderers[i].material = wireMaterial;
        }
    }

    /// <summary>
    /// 共通器具の子 "ElementVisual" に、元素名から決めた色等を適用（Shader で見た目表現）
    /// </summary>
    private void ApplyElementVisual(GameObject vessel, string elementName)
    {
        if (vessel == null) return;

        // ElementVisual を探す（名前一致）
        Transform visualT = null;
        if (!string.IsNullOrEmpty(elementVisualChildName))
            visualT = vessel.transform.Find(elementVisualChildName);

        if (visualT == null)
        {
            // 子孫から名前一致で探索（階層が深い場合）
            var allChildren = vessel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i].name == elementVisualChildName)
                {
                    visualT = allChildren[i];
                    break;
                }
            }
        }

        if (visualT == null)
        {
            Debug.LogWarning($"[Spawner] ElementVisual 子 '{elementVisualChildName}' が見つかりませんでした。");
            return;
        }

        var rend = visualT.GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("[Spawner] ElementVisual に Renderer がありません。");
            return;
        }

        if (elementVisualMaterial == null)
        {
            Debug.LogWarning("[Spawner] elementVisualMaterial が未設定です。");
            return;
        }

        // Renderer.material はランタイムでインスタンス化される（Udon実行可）
        rend.material = elementVisualMaterial;

        // 元素名から安定した色を決定（簡易ハッシュ）
        Color tint = ComputeElementColor(elementName);

        // 共通Shader側で _Color がある前提（なければプロパティ名を合わせてください）
        if (rend.material.HasProperty("_Color"))
            rend.material.SetColor("_Color", tint);

        // 必要なら発光
        if (rend.material.HasProperty("_EmissionColor"))
            rend.material.SetColor("_EmissionColor", tint * 0.5f);
    }

    /// <summary>
    /// 元素名から安定した色を作る（シンプルハッシュ → HSV）
    /// </summary>
    private Color ComputeElementColor(string name)
    {
        if (string.IsNullOrEmpty(name)) return Color.white;

        // 文字列ハッシュ → 0..1
        int h = 0;
        for (int i = 0; i < name.Length; i++) h = (h * 131) ^ name[i];
        float hue = (h & 0xFFFF) / 65535.0f;

        float s = 0.85f;
        float v = 0.95f;
        return Color.HSVToRGB(hue, s, v);
    }
}
