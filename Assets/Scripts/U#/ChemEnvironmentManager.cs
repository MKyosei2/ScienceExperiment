// ChemEnvironmentManager.cs
// UdonSharp制約対応：配列＋プールで2D分子を管理・描画する本体。
// ・Instantiate/new/Destroy/AddComponent/FindObjectOfType 等は一切使用しない
// ・TextMeshProの font/alignment は Inspector で事前設定
// ・公開API：AddAtom/RemoveAtom/SetElementId/SetIsotope/SetCharge/ApplyToShaders
//             AddBond/UpdateBond/RemoveBond/Relayout

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif
using TMPro;

#if CHEM_RUNTIME
[AddComponentMenu("VRC Lab/ChemEnvironmentManager (2D Molecule, Pooled)")]
public class ChemEnvironmentManager : UdonSharpBehaviour
#else
public class ChemEnvironmentManager : MonoBehaviour
#endif
{
    [Header("Rendering (2D)")]
    public Material bondSolidMat;     // 単/二/三結合
    public Material bondDottedMat;    // 水素結合（点線風のマテリアルを事前割当）
    public float atomRadius = 0.22f;
    public int atomCircleSegments = 48;
    public float atomCircleWidth = 0.02f;
    public float bondWidth = 0.03f;
    public float doubleOffset = 0.08f;
    public float tripleOffset = 0.12f;
    public float bondLength = 0.6f;

    [Header("Atom Pool（同じ長さの配列を割当）")]
    public GameObject[] atomRoots;   // スロットのルート（Hierarchyにあらかじめ置き、非表示で用意）
    public LineRenderer[] atomRims;    // 円縁 LineRenderer（事前に付与）
    public TextMeshPro[] atomLabels;  // 元素記号（Inspectorでfont/alignmentを設定）
    public TextMeshPro[] atomAux;     // 同位体/電荷（Inspectorでfont/alignmentを設定）

    [Header("Bond Pool（各結合スロットに最大3本のLineRendererを用意）")]
    public GameObject[] bondRoots;   // 結合ルート（非表示で用意）
    public LineRenderer[] bondLine0;   // 1本目（中心）
    public LineRenderer[] bondLine1;   // 2本目（平行）
    public LineRenderer[] bondLine2;   // 3本目（平行）

    // ---- 内部状態（配列ベース） ----
    private string[] atomIds;
    private string[] atomElements;
    private int[] atomMass;
    private int[] atomCharge;
    private bool[] atomUsed;
    private int atomSlots;

    private string[] bondIds;
    private int[] bondA;       // atom index
    private int[] bondB;       // atom index
    private int[] bondOrder;   // 1/2/3
    private int[] bondType;    // 0=covalent, 1=hydrogen
    private bool[] bondUsed;
    private int bondSlots;

    void Start()
    {
        atomSlots = atomRoots != null ? atomRoots.Length : 0;
        bondSlots = bondRoots != null ? bondRoots.Length : 0;

        atomIds = new string[atomSlots];
        atomElements = new string[atomSlots];
        atomMass = new int[atomSlots];
        atomCharge = new int[atomSlots];
        atomUsed = new bool[atomSlots];

        bondIds = new string[bondSlots];
        bondA = new int[bondSlots];
        bondB = new int[bondSlots];
        bondOrder = new int[bondSlots];
        bondType = new int[bondSlots];
        bondUsed = new bool[bondSlots];

        int seg = atomCircleSegments < 8 ? 8 : atomCircleSegments;

        // Atomプール初期化
        int i;
        for (i = 0; i < atomSlots; i++)
        {
            if (atomRoots[i] != null) atomRoots[i].SetActive(false);
            if (atomRims != null && i < atomRims.Length && atomRims[i] != null)
            {
                LineRenderer rim = atomRims[i];
                rim.useWorldSpace = false; rim.loop = true;
                rim.positionCount = seg;
                rim.startWidth = atomCircleWidth; rim.endWidth = atomCircleWidth;
                rim.numCapVertices = 4; rim.alignment = LineAlignment.View;
                if (bondSolidMat != null) rim.sharedMaterial = bondSolidMat;

                int k;
                for (k = 0; k < seg; k++)
                {
                    float t = (float)k / (float)seg * Mathf.PI * 2f;
                    rim.SetPosition(k, new Vector3(Mathf.Cos(t) * atomRadius, Mathf.Sin(t) * atomRadius, 0f));
                }
            }
        }

        // Bondプール初期化
        for (i = 0; i < bondSlots; i++)
        {
            if (bondRoots[i] != null) bondRoots[i].SetActive(false);
            SetupBondLineDefaults(bondLine0, i);
            SetupBondLineDefaults(bondLine1, i);
            SetupBondLineDefaults(bondLine2, i);
        }
    }

    private void SetupBondLineDefaults(LineRenderer[] arr, int idx)
    {
        if (arr == null) return;
        if (idx >= arr.Length) return;
        LineRenderer lr = arr[idx];
        if (lr == null) return;
        lr.positionCount = 2; lr.useWorldSpace = true;
        lr.startWidth = bondWidth; lr.endWidth = bondWidth; lr.numCapVertices = 6;
        if (bondSolidMat != null) lr.sharedMaterial = bondSolidMat;
        lr.enabled = false;
    }

    // ======= 公開API（実験側から呼ぶ） =======

    // 原子の登録
    public void AddAtom(string id, string element, int massNumber, int charge)
    {
        if (string.IsNullOrEmpty(id)) return;
        int idx = IndexOfAtom(id);
        if (idx >= 0) return;                 // 既にある
        int slot = FirstFreeAtomSlot();
        if (slot < 0) return;                 // プール不足

        atomIds[slot] = id;
        atomElements[slot] = NormalizeSymbol(element);
        atomMass[slot] = massNumber;
        atomCharge[slot] = charge;
        atomUsed[slot] = true;

        if (atomRoots[slot] != null) atomRoots[slot].SetActive(true);
        UpdateAtomVisual(slot);
    }

    public void RemoveAtom(string id)
    {
        int i = IndexOfAtom(id);
        if (i < 0) return;

        // 関連結合を無効化
        int b;
        for (b = 0; b < bondSlots; b++)
        {
            if (bondUsed[b] && (bondA[b] == i || bondB[b] == i))
                RemoveBondByIndex(b);
        }

        atomUsed[i] = false;
        if (atomRoots[i] != null) atomRoots[i].SetActive(false);
        atomIds[i] = null; atomElements[i] = null;
        atomMass[i] = 0; atomCharge[i] = 0;
    }

    // 旧互換を含む更新API
    public void SetElementId(string id, string element)
    {
        int i = IndexOfAtom(id);
        if (i < 0) { AddAtom(id, element, 0, 0); return; }
        atomElements[i] = NormalizeSymbol(element);
        UpdateAtomVisual(i);
    }
    public void SetIsotope(string id, int mass)
    {
        int i = IndexOfAtom(id);
        if (i < 0) return;
        atomMass[i] = mass;
        UpdateAtomVisual(i);
    }
    public void SetCharge(string id, int q)
    {
        int i = IndexOfAtom(id);
        if (i < 0) return;
        atomCharge[i] = q;
        UpdateAtomVisual(i);
    }
    public void ApplyToShaders() { /* 2Dでは特に行うことはない（互換ダミー） */ }

    // 結合
    // type: "covalent" or "hydrogen"
    // order: 1/2/3
    public void AddBond(string id, string aId, string bId, int order, string type)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (IndexOfBond(id) >= 0) return;
        int ai = IndexOfAtom(aId);
        int bi = IndexOfAtom(bId);
        if (ai < 0 || bi < 0) return;

        int slot = FirstFreeBondSlot();
        if (slot < 0) return;

        bondIds[slot] = id;
        bondA[slot] = ai;
        bondB[slot] = bi;
        bondOrder[slot] = order < 1 ? 1 : (order > 3 ? 3 : order);
        bondType[slot] = (type == "hydrogen") ? 1 : 0;
        bondUsed[slot] = true;

        if (bondRoots[slot] != null) bondRoots[slot].SetActive(true);
        UpdateBondVisual(slot);
    }
    public void UpdateBond(string id, int order, string type)
    {
        int i = IndexOfBond(id);
        if (i < 0) return;
        bondOrder[i] = order < 1 ? 1 : (order > 3 ? 3 : order);
        bondType[i] = (type == "hydrogen") ? 1 : 0;
        UpdateBondVisual(i);
    }
    public void RemoveBond(string id)
    {
        int i = IndexOfBond(id);
        if (i < 0) return;
        RemoveBondByIndex(i);
    }

    // レイアウト
    public void Relayout(string hint)
    {
        int count = UsedAtomCount();
        if (count == 0) return;

        // 6員環（全C）
        if (count == 6 && FirstSixAreCarbon())
        {
            Vector3[] ring = RegularPositions(6, bondLength, 90f);
            int i = 0, r = 0;
            for (i = 0; i < atomSlots; i++)
            {
                if (!atomUsed[i]) continue;
                atomRoots[i].transform.localPosition = ring[r++];
            }
            RedrawAllBonds();
            return;
        }

        // 3原子（水型）
        if (count == 3)
        {
            int center = FindFirstNonH();
            if (center >= 0)
            {
                Vector3[] bent = BentPositions(bondLength, 104.5f);
                int i; int placed = 0;
                atomRoots[center].transform.localPosition = Vector3.zero;
                for (i = 0; i < atomSlots; i++)
                {
                    if (!atomUsed[i] || i == center) continue;
                    atomRoots[i].transform.localPosition = bent[placed];
                    placed++;
                    if (placed >= 2) break;
                }
                RedrawAllBonds();
                return;
            }
        }

        // 2原子（直線）
        if (count == 2)
        {
            Vector3[] line = LinearPositions(bondLength);
            int i; int placed = 0;
            for (i = 0; i < atomSlots; i++)
            {
                if (!atomUsed[i]) continue;
                atomRoots[i].transform.localPosition = line[placed];
                placed++;
                if (placed >= 2) break;
            }
            RedrawAllBonds();
            return;
        }

        // 一般：円周等間隔（乱数/フォース法は使わない）
        Vector3[] ring2 = RegularPositions(count, bondLength, 0f);
        int j; int r2 = 0;
        for (j = 0; j < atomSlots; j++)
        {
            if (!atomUsed[j]) continue;
            atomRoots[j].transform.localPosition = ring2[r2++];
        }
        RedrawAllBonds();
    }

    // ======= 内部処理 =======

    private void UpdateAtomVisual(int i)
    {
        if (!atomUsed[i]) return;
        TextMeshPro label = atomLabels[i];
        TextMeshPro aux = atomAux[i];
        LineRenderer rim = atomRims[i];

        if (label != null)
        {
            label.text = atomElements[i];           // どの元素記号でも表示可（任意文字列）
            label.color = ElementColor(atomElements[i]);
        }
        if (aux != null)
        {
            string iso = atomMass[i] > 0 ? atomMass[i].ToString() : "";
            string chg = atomCharge[i] == 0 ? "" : (atomCharge[i] > 0 ? (atomCharge[i].ToString() + "+") : ((-atomCharge[i]).ToString() + "-"));
            aux.text = (iso == "" && chg == "") ? "" : (iso + " " + chg);
        }
        if (rim != null)
        {
            Color c = ElementColor(atomElements[i]);
            rim.startColor = c; rim.endColor = c;
        }
        if (atomRoots[i] != null) atomRoots[i].SetActive(true);
    }

    private void UpdateBondVisual(int bi)
    {
        if (!bondUsed[bi]) return;

        int ai = bondA[bi];
        int bi2 = bondB[bi];
        if (!atomUsed[ai] || !atomUsed[bi2]) { RemoveBondByIndex(bi); return; }

        Vector3 A = atomRoots[ai].transform.position;
        Vector3 B = atomRoots[bi2].transform.position;
        Vector3 dir = (B - A).normalized;
        Vector3 nrm = new Vector3(-dir.y, dir.x, 0f);

        // 3本をいったんOFF
        SetBondLineEnabled(bondLine0, bi, false);
        SetBondLineEnabled(bondLine1, bi, false);
        SetBondLineEnabled(bondLine2, bi, false);

        int ord = bondOrder[bi];
        int t = bondType[bi]; // 0=covalent,1=hydrogen
        Material useMat = (t == 1 && bondDottedMat != null) ? bondDottedMat : bondSolidMat;

        // 中心線
        ApplyBondLine(bondLine0, bi, A, B, 0f, nrm, useMat);

        // 平行線（両側）
        if (ord >= 2) ApplyBondLine(bondLine1, bi, A, B, (doubleOffset * 0.5f), nrm, useMat);
        if (ord >= 3) ApplyBondLine(bondLine2, bi, A, B, -(doubleOffset * 0.5f), nrm, useMat);

        if (bondRoots[bi] != null) bondRoots[bi].SetActive(true);
    }

    private void ApplyBondLine(LineRenderer[] arr, int idx, Vector3 A, Vector3 B, float off, Vector3 nrm, Material m)
    {
        if (arr == null) return;
        if (idx >= arr.Length) return;
        LineRenderer lr = arr[idx];
        if (lr == null) return;

        Vector3 shift = nrm * off;
        lr.enabled = true;
        if (m != null) lr.sharedMaterial = m;
        lr.SetPosition(0, A + shift);
        lr.SetPosition(1, B + shift);
    }
    private void SetBondLineEnabled(LineRenderer[] arr, int idx, bool v)
    {
        if (arr == null) return;
        if (idx >= arr.Length) return;
        if (arr[idx] != null) arr[idx].enabled = v;
    }

    private void RedrawAllBonds()
    {
        int i;
        for (i = 0; i < bondSlots; i++) if (bondUsed[i]) UpdateBondVisual(i);
    }

    private void RemoveBondByIndex(int i)
    {
        if (!bondUsed[i]) return;
        bondUsed[i] = false;
        if (bondRoots[i] != null) bondRoots[i].SetActive(false);
        SetBondLineEnabled(bondLine0, i, false);
        SetBondLineEnabled(bondLine1, i, false);
        SetBondLineEnabled(bondLine2, i, false);

        bondIds[i] = null; bondOrder[i] = 0; bondType[i] = 0;
    }

    // ======= 補助 =======

    private int IndexOfAtom(string id)
    {
        int i;
        for (i = 0; i < atomSlots; i++) if (atomUsed[i] && atomIds[i] == id) return i;
        return -1;
    }
    private int IndexOfBond(string id)
    {
        int i;
        for (i = 0; i < bondSlots; i++) if (bondUsed[i] && bondIds[i] == id) return i;
        return -1;
    }
    private int FirstFreeAtomSlot()
    {
        int i;
        for (i = 0; i < atomSlots; i++) if (!atomUsed[i] && atomRoots[i] != null) return i;
        return -1;
    }
    private int FirstFreeBondSlot()
    {
        int i;
        for (i = 0; i < bondSlots; i++) if (!bondUsed[i] && bondRoots[i] != null) return i;
        return -1;
    }
    private int UsedAtomCount()
    {
        int i, c = 0;
        for (i = 0; i < atomSlots; i++) if (atomUsed[i]) c++;
        return c;
    }
    private bool FirstSixAreCarbon()
    {
        int found = 0; int i;
        for (i = 0; i < atomSlots; i++)
        {
            if (!atomUsed[i]) continue;
            if (atomElements[i] != "C") return false;
            found++;
            if (found == 6) return true;
        }
        return false;
    }
    private int FindFirstNonH()
    {
        int i;
        for (i = 0; i < atomSlots; i++) if (atomUsed[i] && atomElements[i] != "H") return i;
        return -1;
    }
    private static string NormalizeSymbol(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        raw = raw.Trim();
        if (raw.Length == 1) return raw.ToUpperInvariant();
        char c0 = char.ToUpperInvariant(raw[0]);
        string rest = raw.Length > 1 ? raw.Substring(1).ToLowerInvariant() : "";
        return c0 + rest;
    }
    private static Vector3[] LinearPositions(float r)
    {
        Vector3[] v = new Vector3[2];
        v[0] = new Vector3(r, 0, 0);
        v[1] = new Vector3(-r, 0, 0);
        return v;
    }
    private static Vector3[] BentPositions(float r, float angleDeg)
    {
        float a = angleDeg * 0.5f * Mathf.Deg2Rad;
        Vector3[] v = new Vector3[2];
        v[0] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
        v[1] = new Vector3(Mathf.Cos(-a) * r, Mathf.Sin(-a) * r, 0);
        return v;
    }
    private static Vector3[] RegularPositions(int n, float r, float degOffset)
    {
        Vector3[] v = new Vector3[n];
        int i;
        for (i = 0; i < n; i++)
        {
            float th = Mathf.Deg2Rad * (degOffset + i * (360f / n));
            v[i] = new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r, 0f);
        }
        return v;
    }
    private Color ElementColor(string el)
    {
        if (el == "H") return new Color(.74f, .74f, .74f);
        if (el == "C") return new Color(.25f, .25f, .25f);
        if (el == "N") return new Color(.23f, .43f, .65f);
        if (el == "O") return new Color(.89f, .35f, .35f);
        if (el == "S") return new Color(.88f, .77f, .25f);
        if (el == "Cl") return new Color(.38f, .75f, .48f);
        return Color.white;
    }
}
