using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

/// <summary>
/// 元素を押したときにフラスコ・ラベルを生成し、結合情報を管理する
/// </summary>
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("▼ 実験器具Prefab（フラスコ等）")]
    public GameObject[] instrumentPrefabs;

    [Header("▼ ラベルPrefab (TextMeshPro)")]
    public GameObject labelPrefab;
    public Transform labelParent;

    [Header("▼ 結合Prefab")]
    public GameObject singleBondPrefab;
    public GameObject doubleBondPrefab;
    public GameObject tripleBondPrefab;
    public Transform bondParent;

    [Header("▼ 共通見た目制御")]
    public ChemVisualController visualController;

    [Header("▼ AI連携")]
    public AIRequestSender aiSender;

    private GameObject[] activeInstruments = new GameObject[8];
    private GameObject[] activeLabels = new GameObject[32];
    private GameObject[] activeBonds = new GameObject[64];
    private int elementCount = 0;
    private int bondCount = 0;

    private float spacing = 1.2f;
    private Vector2[] directions = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    public void SpawnElementWithButton(GameObject elementButton, int prefabId)
    {
        if (elementButton == null) return;
        string elementName = elementButton.name;

        SpawnElement(prefabId, elementName);

        if (aiSender != null)
        {
            string json = BuildMoleculeJson();
            aiSender.SendMoleculeRequest(json, this);
        }
    }

    private void SpawnElement(int prefabId, string elementLabel)
    {
        if (prefabId < 0 || prefabId >= instrumentPrefabs.Length) return;
        GameObject prefab = instrumentPrefabs[prefabId];
        if (prefab == null) return;

        Vector3 pos = Vector3.zero;
        int baseIndex = -1;

        if (elementCount > 0)
        {
            bool placed = false;
            int safety = 100;
            while (!placed && safety-- > 0)
            {
                baseIndex = Random.Range(0, elementCount);
                GameObject baseLabel = activeLabels[baseIndex];
                if (baseLabel == null) continue;

                Vector2 dir = directions[Random.Range(0, directions.Length)];
                pos = baseLabel.transform.localPosition + new Vector3(dir.x, dir.y, 0) * spacing;

                if (!IsPositionOccupied(pos)) placed = true;
            }
        }

        // ラベル生成
        GameObject label = null;
        if (labelPrefab != null)
        {
            label = VRCInstantiate(labelPrefab);
            if (labelParent != null) label.transform.SetParent(labelParent, false);
            label.transform.localPosition = pos;

            TextMeshPro tmp = label.GetComponent<TextMeshPro>();
            if (tmp != null) tmp.text = elementLabel;

            RegisterLabel(label, elementCount);

            if (baseIndex >= 0)
            {
                GameObject bond = VRCInstantiate(singleBondPrefab);
                if (bondParent != null) bond.transform.SetParent(bondParent, false);
                UpdateBond(bond, activeLabels[baseIndex].transform, label.transform);
                RegisterBond(bond);
            }
        }

        // フラスコ生成
        GameObject instrument = VRCInstantiate(prefab);
        if (instrument != null)
        {
            Vector3 basePos = (labelParent != null) ? labelParent.position : Vector3.zero;
            instrument.transform.position = basePos + new Vector3(0, -2.5f, 0);

            RegisterInstrument(instrument, elementCount);

            if (visualController != null)
            {
                visualController.ApplyElementVisual(instrument, 0, 0.98f, 1.0f);
            }
        }

        elementCount++;
    }

    public void ApplyBondUpdate(int atomA, int atomB, int bondType)
    {
        if (atomA < 0 || atomA >= elementCount) return;
        if (atomB < 0 || atomB >= elementCount) return;

        GameObject prefab = singleBondPrefab;
        if (bondType == 2) prefab = doubleBondPrefab;
        else if (bondType == 3) prefab = tripleBondPrefab;

        GameObject bond = VRCInstantiate(prefab);
        if (bondParent != null) bond.transform.SetParent(bondParent, false);
        UpdateBond(bond, activeLabels[atomA].transform, activeLabels[atomB].transform);
        RegisterBond(bond);
    }

    private void UpdateBond(GameObject bond, Transform a, Transform b)
    {
        Vector3 mid = (a.localPosition + b.localPosition) / 2f;
        Vector3 dir = b.localPosition - a.localPosition;
        float dist = dir.magnitude;

        bond.transform.localPosition = mid;
        bond.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        bond.transform.localScale = new Vector3(0.1f, dist * 0.5f, 0.1f);
    }

    private bool IsPositionOccupied(Vector3 pos)
    {
        for (int i = 0; i < elementCount; i++)
        {
            if (activeLabels[i] != null)
            {
                float dist = Vector3.Distance(activeLabels[i].transform.localPosition, pos);
                if (dist < spacing * 0.5f) return true;
            }
        }
        return false;
    }

    private void RegisterInstrument(GameObject obj, int index)
    {
        EnsureCapacity(index);
        activeInstruments[index] = obj;
    }

    private void RegisterLabel(GameObject obj, int index)
    {
        EnsureCapacity(index);
        activeLabels[index] = obj;
    }

    private void RegisterBond(GameObject obj)
    {
        if (bondCount >= activeBonds.Length)
        {
            GameObject[] newBonds = new GameObject[activeBonds.Length * 2];
            for (int i = 0; i < activeBonds.Length; i++) newBonds[i] = activeBonds[i];
            activeBonds = newBonds;
        }
        activeBonds[bondCount++] = obj;
    }

    private void EnsureCapacity(int index)
    {
        if (index >= activeInstruments.Length)
        {
            int newSize = activeInstruments.Length * 2;
            GameObject[] newInst = new GameObject[newSize];
            GameObject[] newLbl = new GameObject[newSize];
            for (int i = 0; i < activeInstruments.Length; i++)
            {
                newInst[i] = activeInstruments[i];
                newLbl[i] = activeLabels[i];
            }
            activeInstruments = newInst;
            activeLabels = newLbl;
        }
    }

    private string BuildMoleculeJson()
    {
        string json = "{ \"atoms\":[";
        for (int i = 0; i < elementCount; i++)
        {
            if (activeLabels[i] != null)
            {
                string name = activeLabels[i].GetComponent<TextMeshPro>().text;
                json += "\"" + name + "\"";
                if (i < elementCount - 1) json += ",";
            }
        }
        json += "], \"bonds\":[] }";
        return json;
    }

    public string SendMoleculeJson()
    {
        return BuildMoleculeJson();
    }

    public void StartExperiment()
    {
        if (visualController == null) return;
        for (int i = 0; i < elementCount; i++)
        {
            if (activeInstruments[i] != null)
            {
                visualController.ActivateBehaviours(activeInstruments[i]);
            }
        }
    }

    public void ResetExperiment()
    {
        for (int i = 0; i < elementCount; i++)
        {
            if (activeInstruments[i] != null)
            {
                Destroy(activeInstruments[i]);
                activeInstruments[i] = null;
            }
            if (activeLabels[i] != null)
            {
                Destroy(activeLabels[i]);
                activeLabels[i] = null;
            }
        }
        for (int j = 0; j < bondCount; j++)
        {
            if (activeBonds[j] != null)
            {
                Destroy(activeBonds[j]);
                activeBonds[j] = null;
            }
        }
        elementCount = 0;
        bondCount = 0;
    }
}
