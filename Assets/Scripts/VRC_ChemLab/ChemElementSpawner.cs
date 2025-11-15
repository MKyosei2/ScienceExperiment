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

    [Header("=== 見た目（元素カラー適用） ===")]
    [Tooltip("ElementVisual 用マテリアル（Unlit/Color など _Color を持つもの推奨。null なら元マテリアルを使用）")]
    public Material elementVisualMaterial;

    [Tooltip("内側メッシュの子オブジェクト名。見つからなければフラスコ全体に色を塗る")]
    public string elementVisualChildName = "ElementVisual";

    [Header("=== 反応再生 (任意) ===")]
    public JsonReactionPlayer reactionPlayer;

    // 他スクリプト（AIRequestSender / SpawnSelectorButton）から直接触られる想定の変数
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string bondData = "";

    private GameObject[] spawned = new GameObject[128];
    private int spawnedCount = 0;
    private bool hasElementSelected = false;

    // =========================================================
    // 他スクリプト互換用のラッパー（あっても邪魔にならない安全な形）
    // =========================================================
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectEquipment(string name)
    {
        selectedEquipmentName = name;
        SendCustomEvent("_SelectEquipment");
    }
    public void SelectElement(string name)
    {
        selectedElementName = name;
        SendCustomEvent("_SelectElement");
    }
    public void ApplyBondUpdate() { SendCustomEvent("_ApplyBondUpdate"); }

    // =========================================================
    // 器具ボタンを押したとき
    // =========================================================
    public void _SelectEquipment()
    {
        Debug.Log("[Spawner] _SelectEquipment: equipment=" + selectedEquipmentName);

        GameObject g = SpawnOne();
        if (g == null)
        {
            Debug.LogError("[Spawner] _SelectEquipment 中に SpawnOne() が null を返しました。");
            return;
        }

        // 既に元素が選ばれていれば、その色で見た目を適用
        if (hasElementSelected && !string.IsNullOrEmpty(selectedElementName))
        {
            ApplyElementVisual(g, selectedElementName);
        }

        Debug.Log("[Spawner] 器具 '" + selectedEquipmentName + "' で1つ生成");
    }

    // =========================================================
    // 元素ボタンを押したとき
    // =========================================================
    public void _SelectElement()
    {
        Debug.Log("[Spawner] _SelectElement: element=" + selectedElementName);

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
    // 実験開始（PCモード用）
    // =========================================================
    public void _StartExperiment()
    {
        Debug.Log("[Spawner] _StartExperiment");

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
    // リセット（生成した器具と状態の初期化）
    // =========================================================
    public void _ResetExperiment()
    {
        Debug.Log("[Spawner] _ResetExperiment");

        for (int i = 0; i < spawnedCount; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawnedCount = 0;

        selectedElementName = "";
        selectedEquipmentName = "";
        bondData = "";
        hasElementSelected = false;

        Debug.Log("[Spawner] 生成オブジェクトと状態をリセットしました");
    }

    // =========================================================
    // AI から渡された結合情報を反映
    // =========================================================
    public void _ApplyBondUpdate()
    {
        Debug.Log("[Spawner] _ApplyBondUpdate: " + bondData);

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
    // スポーン共通処理
    // =========================================================
    private GameObject SpawnOne()
    {
        if (sourceVessel == null)
        {
            Debug.LogError("[Spawner] sourceVessel が未設定です。CONICAL_FLASK をアサインしてください。");
            return null;
        }

        GameObject inst = VRCInstantiate(sourceVessel);
        if (inst == null)
        {
            // ClientSim / Editor 保険
            inst = Object.Instantiate(sourceVessel);
            Debug.LogWarning("[Spawner] VRCInstantiate が null を返したため、Instantiate で代用しました。");
        }

        if (inst == null)
        {
            Debug.LogError("[Spawner] VRCInstantiate / Instantiate の両方が失敗しました。");
            return null;
        }

        if (spawnParent != null)
            inst.transform.SetParent(spawnParent, false);

        if (spawnedCount < spawned.Length)
        {
            spawned[spawnedCount] = inst;
            spawnedCount++;
        }

        return inst;
    }

    // =========================================================
    // 見た目適用（元素カラー）
    // =========================================================
    private void ApplyElementVisual(GameObject vessel, string elementName)
    {
        if (vessel == null) return;

        // 1) 通常どおり、指定名の子を探す
        Transform visualT = null;

        if (!string.IsNullOrEmpty(elementVisualChildName))
        {
            visualT = vessel.transform.Find(elementVisualChildName);

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
        }

        // 2) 見つからなかった場合は、フラスコ全体を対象にする（効率重視のフォールバック）
        if (visualT == null)
        {
            Debug.LogWarning("[Spawner] 指定された ElementVisual が見つかりませんでした。" +
                             "elementVisualChildName=" + elementVisualChildName +
                             " / vessel=" + vessel.name +
                             "  → フラスコ全体に色を適用します。");
            visualT = vessel.transform;
        }

        Renderer[] rends = visualT.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0)
        {
            Debug.LogWarning("[Spawner] 対象 Transform に Renderer がありません。visualT=" + visualT.name);
            return;
        }

        Color c = ComputeElementColor(elementName);

        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];
            if (r == null) continue;

            // ElementVisual 用マテリアルに差し替え（指定されていれば）
            if (elementVisualMaterial != null)
                r.material = elementVisualMaterial;

            Material m = r.material;
            if (m == null) continue;

            // よくある色プロパティ群
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Tint"))
                m.SetColor("_Tint", c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", c * 1.5f);
                m.EnableKeyword("_EMISSION");
            }
            // WireframeFX.shader 対応
            if (m.HasProperty("_WireColor"))
                m.SetColor("_WireColor", c);
        }

        Debug.Log("[Spawner] ApplyElementVisual: element=" + elementName + " を適用");
    }

    // =========================================================
    // 元素名から色を決める（簡易ハッシュ）
    // =========================================================
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
