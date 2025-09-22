using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("▼ 生成する実験器具（複数登録可）")]
    public GameObject[] instrumentPrefabs;

    [Header("▼ 生成位置/親")]
    public Transform spawnPoint;
    public Transform parentRoot;

    [Header("▼ 共通見た目制御")]
    public ChemVisualController visualController;

    // 動的に増える管理用配列
    private GameObject[] activeInstruments = new GameObject[8];
    private int instrumentCount = 0;

    public void SpawnElement(int prefabId, int elementId)
    {
        if (prefabId < 0 || prefabId >= instrumentPrefabs.Length) return;
        GameObject prefab = instrumentPrefabs[prefabId];
        if (prefab == null) return;

        GameObject instrument = VRCInstantiate(prefab);
        if (instrument == null) return;

        Transform baseTf = (spawnPoint != null) ? spawnPoint : this.transform;
        instrument.transform.SetPositionAndRotation(baseTf.position, baseTf.rotation);
        if (parentRoot != null) instrument.transform.SetParent(parentRoot, true);

        RegisterInstrument(instrument);

        if (visualController != null)
        {
            visualController.ApplyElementVisual(instrument, elementId, 0.98f, 1.0f);
        }
    }

    // 配列に追加（必要なら拡張）
    private void RegisterInstrument(GameObject obj)
    {
        if (instrumentCount >= activeInstruments.Length)
        {
            GameObject[] newArray = new GameObject[activeInstruments.Length * 2];
            for (int i = 0; i < activeInstruments.Length; i++)
            {
                newArray[i] = activeInstruments[i];
            }
            activeInstruments = newArray;
        }

        activeInstruments[instrumentCount] = obj;
        instrumentCount++;
    }

    // 共通「実験開始ボタン」から呼ばれる
    public void StartExperiment()
    {
        if (visualController == null) return;

        for (int i = 0; i < instrumentCount; i++)
        {
            GameObject inst = activeInstruments[i];
            if (inst != null)
            {
                visualController.ActivateBehaviours(inst);
            }
        }
    }

    // 共通「リセットボタン」から呼ばれる
    public void ResetExperiment()
    {
        for (int i = 0; i < instrumentCount; i++)
        {
            GameObject inst = activeInstruments[i];
            if (inst != null)
            {
                Destroy(inst);
                activeInstruments[i] = null;
            }
        }
        instrumentCount = 0;
    }
}
