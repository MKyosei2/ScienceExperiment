using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("VRC Lab/ChemElementSpawner")]
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("Spawn Root (生成先)")]
    public Transform spawnParent;  // Systems/Spawner 推奨

    [Header("Prefab設定")]
    [Tooltip("元素ボタンを押したときに使う共通器具Prefab（例：CONICAL_FLASK）")]
    public GameObject defaultElementVesselPrefab;

    [Tooltip("器具ボタンで選択できる実験器具Prefab。名前一致で選択される。")]
    public GameObject[] equipmentPrefabs;

    [Header("マテリアル設定")]
    public Material wireMaterial;           // ワイヤーフレーム用
    public Material elementVisualMaterial;  // 元素の見た目用 (_Colorを持つこと)
    public string elementVisualChildName = "ElementVisual";

    [Header("反応システム")]
    public JsonReactionPlayer reactionPlayer;

    // 状態保持（他スクリプトとやりとりする用）
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string bondData = "";

    // いまシーンに出ている実体
    private GameObject currentVessel;
    private bool hasElementSelected = false;

    // ========== 互換メソッド（既存スクリプトが呼んでも動くように） ==========
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectEquipment(string name) { selectedEquipmentName = name; SendCustomEvent("_SelectEquipment"); }
    public void SelectElement(string name) { selectedElementName = name; SendCustomEvent("_SelectElement"); }
    public void SpawnSelectedVesselAndStart() { SendCustomEvent("_StartExperiment"); }

    // =====================================================
    // 器具ボタンを押したとき
    // =====================================================
    public void _SelectEquipment()
    {
        if (string.IsNullOrEmpty(selectedEquipmentName))
        {
            Debug.LogWarning("[Spawner] 器具名が空です。");
            return;
        }

        GameObject prefab = FindPrefabByName(equipmentPrefabs, selectedEquipmentName);
        if (prefab == null)
        {
            Debug.LogWarning($"[Spawner] 器具 '{selectedEquipmentName}' が見つかりませんでした。");
            return;
        }

        // すでに何か出ていたら消す
        if (currentVessel != null)
        {
            Destroy(currentVessel);
            currentVessel = null;
        }

        // Prefabを生成（UdonSharp対応版）
        currentVessel = SafeInstantiate(prefab);
        if (currentVessel == null)
        {
            Debug.LogError("[Spawner] 器具Prefabの生成に失敗しました。Prefab参照またはspawnParentを確認してください。");
            return;
        }

        PlaceUnderSpawnParent(currentVessel);
        ApplyWireframe(currentVessel);

        // 先に元素が選ばれていたら表示を載せる
        if (hasElementSelected && !string.IsNullOrEmpty(selectedElementName))
        {
            ApplyElementVisual(currentVessel, selectedElementName);
        }

        Debug.Log($"[Spawner] 器具 '{selectedEquipmentName}' を生成しました。");
    }

    // =====================================================
    // 元素ボタンを押したとき
    // =====================================================
    public void _SelectElement()
    {
        if (string.IsNullOrEmpty(selectedElementName))
        {
            Debug.LogWarning("[Spawner] 元素名が空です。");
            return;
        }

        hasElementSelected = true;

        // 器具がまだ出ていないなら、共通器具を生成
        if (currentVessel == null)
        {
            if (defaultElementVesselPrefab == null)
            {
                Debug.LogError("[Spawner] defaultElementVesselPrefab が未設定です。");
                return;
            }

            currentVessel = SafeInstantiate(defaultElementVesselPrefab);
            if (currentVessel == null)
            {
                Debug.LogError("[Spawner] 共通器具Prefabの生成に失敗しました。");
                return;
            }

            PlaceUnderSpawnParent(currentVessel);
            ApplyWireframe(currentVessel);

            Debug.Log("[Spawner] 器具がなかったので共通器具Prefabを生成しました。");
        }

        // 器具の中に元素の見た目を出す
        ApplyElementVisual(currentVessel, selectedElementName);
        Debug.Log($"[Spawner] 元素 '{selectedElementName}' を現在の器具に表示しました。");
    }

    // =====================================================
    // PCモードのStartボタン
    // =====================================================
    public void _StartExperiment()
    {
        if (currentVessel == null)
        {
            Debug.LogWarning("[Spawner] 実験を開始できません：器具が存在しません（元素か器具ボタンを先に押してください）");
            return;
        }

        if (!string.IsNullOrEmpty(bondData) && reactionPlayer != null)
        {
            reactionPlayer.Play(bondData);
            Debug.Log("[Spawner] 実験を実行（AI反応を適用）");
        }
        else
        {
            Debug.Log("[Spawner] 実験を実行しました（AIデータなし）");
        }
    }

    // =====================================================
    // リセット
    // =====================================================
    public void _ResetExperiment()
    {
        if (currentVessel != null)
        {
            Destroy(currentVessel);
            currentVessel = null;
        }

        selectedElementName = "";
        selectedEquipmentName = "";
        hasElementSelected = false;

        Debug.Log("[Spawner] 実験リセット完了（生成した器具を削除）");
    }

    // =====================================================
    // AI反応の適用
    // =====================================================
    public void _ApplyBondUpdate()
    {
        if (string.IsNullOrEmpty(bondData))
        {
            Debug.LogWarning("[Spawner] bondData が空のため適用スキップ");
            return;
        }

        if (reactionPlayer != null)
            reactionPlayer.Play(bondData);

        Debug.Log($"[Spawner] AI反応を適用: {bondData}");
    }

    // =====================================================
    // 内部補助
    // =====================================================

    // UdonSharpで使えるフォールバック版：例外は使わない
    private GameObject SafeInstantiate(GameObject prefab)
    {
        if (prefab == null) return null;

        // まずはVRChat側の生成を試す
        GameObject obj = VRCInstantiate(prefab);
        if (obj != null) return obj;

        // Editor などで VRCInstantiate が null だった場合に普通の Instantiate で代わりに生成
        obj = Object.Instantiate(prefab);
        return obj;
    }

    private void PlaceUnderSpawnParent(GameObject obj)
    {
        if (obj == null) return;
        if (spawnParent != null)
        {
            obj.transform.SetParent(spawnParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }
    }

    private GameObject FindPrefabByName(GameObject[] list, string name)
    {
        if (list == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < list.Length; i++)
        {
            var g = list[i];
            if (g != null && g.name == name)
                return g;
        }
        return null;
    }

    private void ApplyWireframe(GameObject root)
    {
        if (wireMaterial == null || root == null) return;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = wireMaterial;
    }

    private void ApplyElementVisual(GameObject vessel, string elementName)
    {
        if (vessel == null) return;

        Transform visualT = vessel.transform.Find(elementVisualChildName);
        if (visualT == null)
        {
            var all = vessel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == elementVisualChildName)
                {
                    visualT = all[i];
                    break;
                }
            }
        }

        if (visualT == null)
        {
            Debug.LogWarning($"[Spawner] ElementVisual '{elementVisualChildName}' が見つかりません。");
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

        rend.material = elementVisualMaterial;

        Color c = ComputeElementColor(elementName);
        if (rend.material.HasProperty("_Color"))
            rend.material.SetColor("_Color", c);
        if (rend.material.HasProperty("_EmissionColor"))
            rend.material.SetColor("_EmissionColor", c * 0.5f);
    }

    private Color ComputeElementColor(string name)
    {
        if (string.IsNullOrEmpty(name)) return Color.white;
        int h = 0;
        for (int i = 0; i < name.Length; i++)
            h = (h * 131) ^ name[i];
        float hue = (h & 0xFFFF) / 65535f;
        return Color.HSVToRGB(hue, 0.85f, 0.95f);
    }
}
