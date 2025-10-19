using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("VRC Lab/ChemElementSpawner")]
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnParent;
    public GameObject conicalFlaskPrefab;
    public GameObject beakerPrefab;
    public Material wireMaterial;
    public GameObject[] elementPrefabs;
    public JsonReactionPlayer reactionPlayer;

    [HideInInspector] public string bondData = "";

    // 選択された装置名・元素名（外部アクセス許可）
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string selectedElementName = "";

    private GameObject selectedEquipment;
    private GameObject selectedElement;
    private GameObject[] spawnedObjects = new GameObject[32];
    private int spawnCount = 0;

    // ===================== 旧互換メソッド（他スクリプト呼び出し用） =====================
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectEquipment(string n)
    {
        selectedEquipmentName = n;
        SendCustomEvent("_SelectEquipment");
    }
    public void SelectElement(string n)
    {
        selectedElementName = n;
        SendCustomEvent("_SelectElement");
    }
    public void SpawnSelectedVesselAndStart() { SendCustomEvent("_StartExperiment"); }

    // ===================== UdonSharp実装 =====================

    // 器具選択
    public void _SelectEquipment()
    {
        string name = selectedEquipmentName;
        if (name == "ConicalFlask") selectedEquipment = conicalFlaskPrefab;
        else if (name == "Beaker") selectedEquipment = beakerPrefab;
        else selectedEquipment = conicalFlaskPrefab;
        Debug.Log($"[Spawner] 器具 '{name}' 選択");
    }

    // 元素選択
    public void _SelectElement()
    {
        string name = selectedElementName;
        foreach (GameObject g in elementPrefabs)
        {
            if (g != null && g.name == name)
            {
                selectedElement = g;
                Debug.Log($"[Spawner] 元素 '{name}' 選択");
                return;
            }
        }
        Debug.LogWarning($"[Spawner] 元素 '{name}' が見つかりません");
    }

    // 実験開始
    public void _StartExperiment()
    {
        if (selectedEquipment == null || selectedElement == null)
        {
            Debug.LogWarning("[Spawner] 器具または元素が未選択");
            return;
        }

        GameObject vessel = VRCInstantiate(selectedEquipment);
        vessel.transform.SetParent(spawnParent);
        vessel.transform.localPosition = Vector3.zero;
        AddSpawn(vessel);

        GameObject elem = VRCInstantiate(selectedElement);
        elem.transform.SetParent(vessel.transform);
        elem.transform.localPosition = new Vector3(0, 0.05f, 0);
        AddSpawn(elem);

        ApplyWireframe(vessel);
        ApplyWireframe(elem);

        Debug.Log($"[Spawner] 実験開始: {selectedElement.name} × {selectedEquipment.name}");
    }

    // 実験リセット
    public void _ResetExperiment()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            if (spawnedObjects[i] != null)
                Destroy(spawnedObjects[i]);
        }
        spawnCount = 0;
        selectedElement = null;
        Debug.Log("[Spawner] 実験リセット完了");
    }

    // ワイヤーフレーム適用
    private void ApplyWireframe(GameObject obj)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null && wireMaterial != null)
        {
            r.material = wireMaterial;
        }
    }

    private void AddSpawn(GameObject obj)
    {
        if (spawnCount >= spawnedObjects.Length) return;
        spawnedObjects[spawnCount++] = obj;
    }

    // ==== AI応答（結合情報）適用 ====
    public void _ApplyBondUpdate()
    {
        if (string.IsNullOrEmpty(bondData))
        {
            Debug.LogWarning("[Spawner] bondData が空");
            return;
        }
        Debug.Log($"[Spawner] AI反応を適用: {bondData}");

        if (reactionPlayer != null)
        {
            reactionPlayer.Play(bondData);
        }
    }
}
