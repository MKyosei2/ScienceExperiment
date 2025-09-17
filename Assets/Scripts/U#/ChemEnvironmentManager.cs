// ChemEnvironmentManager.cs
// - UdonSharp対応版
// - 元素ボタンでフラスコPrefabと空中ラベルを生成
// - AtomPrefab/BondPrefabをInstantiateして増やせる
// - 最大: 原子50個 / 結合100本
// - 旧API互換を維持

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif
using TMPro;

#if CHEM_RUNTIME
[AddComponentMenu("VRC Lab/ChemEnvironmentManager (Final Version)")]
public class ChemEnvironmentManager : UdonSharpBehaviour
#else
public class ChemEnvironmentManager : MonoBehaviour
#endif
{
    [Header("Player View")]
    public Transform playerView;

    [Header("Prefabs")]
    public string[] elementKeys;
    public GameObject[] flaskPrefabs;
    public GameObject defaultFlaskPrefab;
    public GameObject elementLabelPrefab;
    public GameObject atomPrefab; // 小さなAtom(TextMeshProつき)
    public GameObject bondPrefab; // Bond(LineRendererつき)

    [Header("Parents")]
    public Transform flaskParent;
    public Transform labelParent;
    public Transform atomsParent;
    public Transform bondsParent;

    [Header("Max Counts (固定)")]
    public int maxAtoms = 50;
    public int maxBonds = 100;

    // 内部
    private GameObject activeLabel;

    private string[] atomIds;
    private GameObject[] atomObjs;
    private int atomCount = 0;

    private GameObject[] bondObjs;
    private string[] bondA;
    private string[] bondB;
    private int bondCount = 0;

    void Start()
    {
        atomIds = new string[maxAtoms];
        atomObjs = new GameObject[maxAtoms];

        bondObjs = new GameObject[maxBonds];
        bondA = new string[maxBonds];
        bondB = new string[maxBonds];
    }

    void Update()
    {
        if (activeLabel != null && playerView != null)
        {
            Vector3 forward = playerView.forward;
            activeLabel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    // ====== フラスコ生成 ======
    public void SpawnFlaskLook(string elementSym)
    {
        int idx = -1;
        for (int i = 0; i < elementKeys.Length; i++)
        {
            if (elementKeys[i] == elementSym) { idx = i; break; }
        }

        GameObject prefab = null;
        if (idx >= 0 && flaskPrefabs != null && idx < flaskPrefabs.Length)
            prefab = flaskPrefabs[idx];
        if (prefab == null) prefab = defaultFlaskPrefab;

        if (prefab != null && flaskParent != null)
        {
            GameObject go = Instantiate(prefab, flaskParent);
            go.SetActive(true);
        }
    }

    // ====== ラベル生成 ======
    private void SpawnOrUpdateLabel(string elementSym)
    {
        if (elementLabelPrefab == null || labelParent == null) return;

        if (activeLabel == null)
        {
            activeLabel = Instantiate(elementLabelPrefab, labelParent);
        }

        TextMeshPro tmp = activeLabel.GetComponent<TextMeshPro>();
        if (tmp != null) tmp.text = elementSym;
        activeLabel.SetActive(true);
    }

    // ====== Atom管理 ======
    private int FindAtomIndex(string id)
    {
        for (int i = 0; i < atomCount; i++)
        {
            if (atomIds[i] == id) return i;
        }
        return -1;
    }

    public void AddAtom(string id, string element, int massNumber, int charge)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(element)) return;
        if (FindAtomIndex(id) >= 0) return;
        if (atomCount >= maxAtoms) return;
        if (atomPrefab == null || atomsParent == null) return;

        GameObject atomObj = Instantiate(atomPrefab, atomsParent);
        atomObj.SetActive(true);

        TextMeshPro tmp = atomObj.GetComponentInChildren<TextMeshPro>();
        if (tmp != null) tmp.text = element;

        atomIds[atomCount] = id;
        atomObjs[atomCount] = atomObj;
        atomCount++;

        SpawnFlaskLook(element);
        SpawnOrUpdateLabel(element);
    }

    public void SetElementId(string id, string element)
    {
        int idx = FindAtomIndex(id);
        if (idx < 0) return;
        TextMeshPro tmp = atomObjs[idx].GetComponentInChildren<TextMeshPro>();
        if (tmp != null) tmp.text = element;
        SpawnOrUpdateLabel(element);
    }

    public void SetIsotope(string id, int mass) { }
    public void SetCharge(string id, int q) { }
    public void ApplyToShaders() { }

    // ====== Bond管理 ======
    public void AddBond(string idA, string idB, int order)
    {
        int a = FindAtomIndex(idA);
        int b = FindAtomIndex(idB);
        if (a < 0 || b < 0) return;
        if (bondCount >= maxBonds) return;
        if (bondPrefab == null || bondsParent == null) return;

        GameObject bondObj = Instantiate(bondPrefab, bondsParent);
        LineRenderer lr = bondObj.GetComponent<LineRenderer>();
        if (lr != null)
        {
            Vector3 posA = atomObjs[a].transform.localPosition;
            Vector3 posB = atomObjs[b].transform.localPosition;
            lr.positionCount = 2;
            lr.SetPosition(0, posA);
            lr.SetPosition(1, posB);
            lr.startColor = lr.endColor = Color.white;
            lr.startWidth = lr.endWidth = 0.02f;
        }

        bondObjs[bondCount] = bondObj;
        bondA[bondCount] = idA;
        bondB[bondCount] = idB;
        bondCount++;
    }

    public void Relayout(string hint)
    {
        float r = 0.5f;
        for (int i = 0; i < atomCount; i++)
        {
            float th = Mathf.PI * 2f * i / Mathf.Max(atomCount, 1);
            atomObjs[i].transform.localPosition = new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, 0);
        }

        for (int i = 0; i < bondCount; i++)
        {
            LineRenderer lr = bondObjs[i].GetComponent<LineRenderer>();
            if (lr != null)
            {
                int a = FindAtomIndex(bondA[i]);
                int b = FindAtomIndex(bondB[i]);
                if (a >= 0 && b >= 0)
                {
                    Vector3 posA = atomObjs[a].transform.localPosition;
                    Vector3 posB = atomObjs[b].transform.localPosition;
                    lr.SetPosition(0, posA);
                    lr.SetPosition(1, posB);
                }
            }
        }
    }

    // ====== Commit系 ======
    public string pendingAtomId;
    public string pendingElement;

    public void CommitAddAtom()
    {
        AddAtom(pendingAtomId, pendingElement, 0, 0);
        Relayout("auto");
    }

    // ====== Reset ======
    public void ResetAll()
    {
        if (flaskParent != null)
        {
            foreach (Transform c in flaskParent) Destroy(c.gameObject);
        }
        if (activeLabel != null) Destroy(activeLabel);

        for (int i = 0; i < atomCount; i++) Destroy(atomObjs[i]);
        atomCount = 0;

        for (int i = 0; i < bondCount; i++) Destroy(bondObjs[i]);
        bondCount = 0;
    }
}
