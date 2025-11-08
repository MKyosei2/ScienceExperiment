using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("VRC Lab/ChemElementSpawner")]
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("=== 生成先 ===")]
    [Tooltip("生成した器具をぶら下げる親。例: Systems/Spawner")]
    public Transform spawnParent;

    [Header("=== 共通器具 ===")]
    [Tooltip("CONICAL_FLASK など、もとになる器具 (Prefab でもシーンオブジェクトでもOK)")]
    public GameObject sourceVessel;

    [Header("=== 見た目 ===")]
    public Material wireMaterial;
    public Material elementVisualMaterial;
    public string elementVisualChildName = "ElementVisual";

    [Header("=== 反応再生 (任意) ===")]
    public JsonReactionPlayer reactionPlayer;

    // ボタン側から渡される情報
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string bondData = "";

    private GameObject[] spawned = new GameObject[128];
    private int spawnedCount = 0;
    private bool hasElementSelected = false;

    // ---------- 互換用ラッパー ----------
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectEquipment(string name) { selectedEquipmentName = name; SendCustomEvent("_SelectEquipment"); }
    public void SelectElement(string name) { selectedElementName = name; SendCustomEvent("_SelectElement"); }
    public void SpawnSelectedVesselAndStart() { SendCustomEvent("_StartExperiment"); }
    public void ApplyBondUpdate() { SendCustomEvent("_ApplyBondUpdate"); }

    // =========================================================
    // 器具ボタンを押したとき
    // =========================================================
    public void _SelectEquipment()
    {
        Debug.Log("[Spawner] _SelectEquipment 受信: equipment=" + selectedEquipmentName);

        GameObject g = SpawnOne();
        if (g == null)
        {
            Debug.LogError("[Spawner] _SelectEquipment 中に SpawnOne() が null を返しました。");
            return;
        }

        ApplyWireframe(g);

        if (hasElementSelected && !string.IsNullOrEmpty(selectedElementName))
            ApplyElementVisual(g, selectedElementName);

        Debug.Log("[Spawner] 器具 '" + selectedEquipmentName + "' で1つ生成");
    }

    // =========================================================
    // 元素ボタンを押したとき
    // =========================================================
    public void _SelectElement()
    {
        Debug.Log("[Spawner] _SelectElement 受信: element=" + selectedElementName +
                  ", source=" + (sourceVessel != null ? sourceVessel.name : "NULL") +
                  ", parent=" + (spawnParent != null ? spawnParent.name : "NULL"));

        hasElementSelected = true;

        GameObject g = SpawnOne();
        if (g == null)
        {
            Debug.LogError("[Spawner] _SelectElement 中に SpawnOne() が null を返しました。");
            return;
        }

        ApplyElementVisual(g, selectedElementName);

        Debug.Log("[Spawner] 元素 '" + selectedElementName + "' で1つ生成");
    }

    // =========================================================
    // 実験開始（PCモード）
    // =========================================================
    public void _StartExperiment()
    {
        Debug.Log("[Spawner] _StartExperiment 呼び出し");

        if (!string.IsNullOrEmpty(bondData) && reactionPlayer != null)
        {
            reactionPlayer.Play(bondData);
            Debug.Log("[Spawner] reactionPlayer.Play を実行");
        }
        else
        {
            Debug.Log("[Spawner] bondData なし or reactionPlayer 未設定のため再生スキップ");
        }
    }

    // =========================================================
    // リセット（生成したオブジェクト全削除）
    // =========================================================
    public void _ResetExperiment()
    {
        Debug.Log("[Spawner] _ResetExperiment 呼び出し");

        for (int i = 0; i < spawnedCount; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawnedCount = 0;

        selectedElementName = "";
        selectedEquipmentName = "";
        hasElementSelected = false;

        Debug.Log("[Spawner] 生成オブジェクトを全削除しました");
    }

    // =========================================================
    // AI からの更新を適用
    // =========================================================
    public void _ApplyBondUpdate()
    {
        Debug.Log("[Spawner] _ApplyBondUpdate 呼び出し: " + bondData);

        if (string.IsNullOrEmpty(bondData))
        {
            Debug.LogWarning("[Spawner] bondData が空なのでスキップ");
            return;
        }

        if (reactionPlayer != null)
        {
            reactionPlayer.Play(bondData);
            Debug.Log("[Spawner] reactionPlayer.Play を実行");
        }
        else
        {
            Debug.LogWarning("[Spawner] reactionPlayer 未設定のため AI 反応を再生できません");
        }
    }

    // =========================================================
    // 内部：1個スポーン（ここが一番重要）
    // =========================================================
    private GameObject SpawnOne()
    {
        if (sourceVessel == null)
        {
            Debug.LogError("[Spawner] sourceVessel が Inspector で未設定です。");
            return null;
        }

        Debug.Log("[Spawner] SpawnOne 開始: source=" + sourceVessel.name +
                  ", activeSelf=" + sourceVessel.activeSelf);

        GameObject inst = null;

        // まず VRCInstantiate を試す（VRChat本番）
        inst = VRCInstantiate(sourceVessel);

        if (inst == null)
        {
            // Editor / ClientSim などで VRCInstantiate がうまく動かない場合の保険
            inst = (GameObject)Object.Instantiate(sourceVessel);
            Debug.LogWarning("[Spawner] VRCInstantiate が null を返したため、Object.Instantiate で代用しました。");
        }

        if (inst == null)
        {
            Debug.LogError("[Spawner] VRCInstantiate / Instantiate の両方で null が返されました。");
            return null;
        }

        // 必ず有効化
        if (!inst.activeSelf)
            inst.SetActive(true);

        // 親の下に入れる
        if (spawnParent != null)
        {
            inst.transform.SetParent(spawnParent, false);
        }

        // 管理用配列に登録
        if (spawnedCount < spawned.Length)
        {
            spawned[spawnedCount] = inst;
            spawnedCount++;
        }

        Debug.Log("[Spawner] SpawnOne 完了: 実体 " + inst.name +
                  " / parent=" + (inst.transform.parent != null ? inst.transform.parent.name : "NULL"));

        return inst;
    }

    // =========================================================
    // 見た目適用（ワイヤフレーム）
    // =========================================================
    private void ApplyWireframe(GameObject root)
    {
        if (wireMaterial == null || root == null) return;

        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            rends[i].material = wireMaterial;
        }
    }

    // =========================================================
    // 見た目適用（元素カラー）
    // =========================================================
    private void ApplyElementVisual(GameObject vessel, string elementName)
    {
        if (vessel == null) return;

        Transform visualT = vessel.transform.Find(elementVisualChildName);
        if (visualT == null)
        {
            Transform[] all = vessel.GetComponentsInChildren<Transform>(true);
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
            Debug.LogWarning("[Spawner] ElementVisual '" + elementVisualChildName + "' が見つかりません。");
            return;
        }

        Renderer rend = visualT.GetComponent<Renderer>();
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
