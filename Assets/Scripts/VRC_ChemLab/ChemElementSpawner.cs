using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("VRC Lab/ChemElementSpawner")]
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("生成先")]
    public Transform spawnParent;

    [Header("生成元 (シーンに1個だけ置く。非アクティブでもOK)")]
    [Tooltip("CONICAL_FLASKなど、シーンに1つだけ置いておく元オブジェクト。非アクティブでもOK。")]
    public GameObject sourceVessel;

    [Header("見た目まわり")]
    public Material wireMaterial;
    public Material elementVisualMaterial;
    public string elementVisualChildName = "ElementVisual";

    [Header("反応システム(任意)")]
    public JsonReactionPlayer reactionPlayer;

    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string bondData = "";

    private GameObject[] spawned = new GameObject[128];
    private int spawnedCount = 0;
    private bool hasElementSelected = false;

    // ---------------------- 外部呼び出し互換 ----------------------
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectEquipment(string name) { selectedEquipmentName = name; SendCustomEvent("_SelectEquipment"); }
    public void SelectElement(string name) { selectedElementName = name; SendCustomEvent("_SelectElement"); }
    public void SpawnSelectedVesselAndStart() { SendCustomEvent("_StartExperiment"); }

    // ===============================================================
    // 器具ボタンを押したとき
    // ===============================================================
    public void _SelectEquipment()
    {
        GameObject g = SpawnOne();
        if (g == null) return;

        ApplyWireframe(g);

        if (hasElementSelected && !string.IsNullOrEmpty(selectedElementName))
            ApplyElementVisual(g, selectedElementName);

        Debug.Log($"[Spawner] 器具ボタンで1つ生成（{selectedEquipmentName}）");
    }

    // ===============================================================
    // 元素ボタンを押したとき
    // ===============================================================
    public void _SelectElement()
    {
        hasElementSelected = true;

        GameObject g = SpawnOne();
        if (g == null) return;

        ApplyElementVisual(g, selectedElementName);
        Debug.Log($"[Spawner] 元素 '{selectedElementName}' で1つ生成");
    }

    // ===============================================================
    // 実験開始（PCモード専用）
    // ===============================================================
    public void _StartExperiment()
    {
        if (!string.IsNullOrEmpty(bondData) && reactionPlayer != null)
            reactionPlayer.Play(bondData);
        else
            Debug.Log("[Spawner] 実験を実行しました（AIデータなし）");
    }

    // ===============================================================
    // リセット（全削除）
    // ===============================================================
    public void _ResetExperiment()
    {
        for (int i = 0; i < spawnedCount; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawnedCount = 0;

        selectedElementName = "";
        selectedEquipmentName = "";
        hasElementSelected = false;

        Debug.Log("[Spawner] リセット：生成したオブジェクトを全削除しました。");
    }

    // ===============================================================
    // 安全に生成（非アクティブな元でもOK）
    // ===============================================================
    private GameObject SpawnOne()
    {
        if (sourceVessel == null)
        {
            Debug.LogError("[Spawner] sourceVessel が設定されていません。");
            return null;
        }

        bool wasActive = sourceVessel.activeSelf;
        if (!wasActive) sourceVessel.SetActive(true);

        GameObject inst = VRCInstantiate(sourceVessel);
        if (inst == null)
        {
            inst = Object.Instantiate(sourceVessel);
        }

        if (!wasActive) sourceVessel.SetActive(false);

        if (inst == null)
        {
            Debug.LogError("[Spawner] 生成に失敗しました。");
            return null;
        }

        if (spawnParent != null)
        {
            inst.transform.SetParent(spawnParent);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }

        if (spawnedCount < spawned.Length)
            spawned[spawnedCount++] = inst;

        return inst;
    }

    // ===============================================================
    // ワイヤー表示
    // ===============================================================
    private void ApplyWireframe(GameObject root)
    {
        if (wireMaterial == null || root == null) return;
        var rends = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
            rends[i].material = wireMaterial;
    }

    // ===============================================================
    // 元素見た目
    // ===============================================================
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
