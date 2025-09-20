// ChemEnvironmentManager.cs
// 役割：
// 1) 元素選択時に CONICAL_FLASK を表示（WireframeFX の液体を満タン＆元素色で可視化）
// 2) 二枚目のイメージ図（空中ビルボード）を表示・更新
// Udon対応：Instantiate は VRCInstantiate を使用。未公開API（new/Destroy/FindObjectOfType 等）は使わない。

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
using VRC.SDKBase;
#endif

#if CHEM_RUNTIME
[AddComponentMenu("VRC Lab/ChemEnvironmentManager (Udon/WireframeFX)")]
public class ChemEnvironmentManager : UdonSharpBehaviour
#else
public class ChemEnvironmentManager : MonoBehaviour
#endif
{
    [Header("=== Prefabs / Parents ===")]
    [Tooltip("CONICAL_FLASK のデフォルト Prefab（WireframeFX シェーダ使用）")]
    public GameObject defaultFlaskPrefab;

    [Tooltip("元素ごとにフラスコ見た目が分かれているなら設定（省略可）")]
    public GameObject[] flaskPrefabs;

    [Tooltip("フラスコをぶら下げる親")]
    public Transform flaskParent;

    [Header("=== Overlay (2枚目) ===")]
    [Tooltip("二枚目のイメージ図をぶら下げる親")]
    public Transform overlayParent;

    [Tooltip("二枚目用 Quad/Plane（Unlit系、_MainTex あり）")]
    public GameObject overlayBillboardPrefab;

    [Header("=== Player/View ===")]
    [Tooltip("カメラ/プレイヤーの Transform（ビルボード位置・向きに使用）")]
    public Transform playerView;

    [Header("=== Element Mapping ===")]
    [Tooltip("元素キー（例: H, O, Na, ...）")]
    public string[] elementKeys;

    [Tooltip("各元素の液体カラー（WireframeFX の _LiquidColor に反映）")]
    public Color[] elementLiquidColors;

    [Tooltip("各元素の二枚目画像（Sprite の texture を使用）")]
    public Sprite[] elementOverlaySprites;

    [Header("=== Overlay Settings ===")]
    public float overlayDistance = 1.2f;
    public Vector2 overlaySize = new Vector2(0.6f, 0.4f);

    // ---- 内部状態 ----
    private GameObject _activeFlask;          // 生成して再利用
    private GameObject _activeOverlay;        // 生成して再利用
    private Renderer _activeOverlayRenderer; // overlay の Renderer キャッシュ
    private string _lastElementSym = "";

    // WireframeFX シェーダ名（参照用）
    private const string kWireShaderName = "VRChat/ChemGlass_Universal_Slosh_Wire";

    void Update()
    {
        // 二枚目（ビルボード）をプレイヤーの方へ
        if (playerView != null && _activeOverlay != null)
        {
            Vector3 pos = playerView.position + playerView.forward * overlayDistance;
            _activeOverlay.transform.position = pos;
            _activeOverlay.transform.rotation =
                Quaternion.LookRotation(playerView.forward, Vector3.up);
        }
    }

    // ========= 外部API =========

    /// <summary>UIから元素が選ばれたときに呼ぶ</summary>
    public void SetElementId(string id, string element)
    {
        // id は拡張用。Udonで生成APIが限られるため、ここでは表示のみを更新。
        _lastElementSym = element;

        // 1) フラスコ表示＋液体適用
        SpawnOrShowFlask(element);

        // 2) 二枚目のイメージ更新
        SpawnOrUpdateOverlay(element);
    }

    /// <summary>直近の元素表示を強制再適用（PCモードの“実験開始”等から呼ぶ想定）</summary>
    public void ApplyToShaders()
    {
        if (_activeFlask == null || string.IsNullOrEmpty(_lastElementSym)) return;
        ApplyWireShaderParams(_activeFlask, _lastElementSym);
        SpawnOrUpdateOverlay(_lastElementSym);
    }

    /// <summary>PC/VR共通のリセットボタンから呼ぶ</summary>
    public void ResetAll()
    {
        if (_activeFlask != null) _activeFlask.SetActive(false);
        if (_activeOverlay != null) _activeOverlay.SetActive(false);
        _lastElementSym = "";
    }

    // ========= 表示系内部実装 =========

    private void SpawnOrShowFlask(string elementSym)
    {
        if (flaskParent == null) return;

        // まだ生成していなければ VRCInstantiate（Udon対応）
        if (_activeFlask == null)
        {
            GameObject prefab = ResolveFlaskPrefab(elementSym);
            if (prefab == null) return;

            _activeFlask = VRCInstantiate(prefab);
            if (_activeFlask == null) return;

            _activeFlask.transform.SetParent(flaskParent, false);
        }
        else
        {
            _activeFlask.SetActive(true);
        }

        // WireframeFX のパラメータを適用
        ApplyWireShaderParams(_activeFlask, elementSym);
    }

    private GameObject ResolveFlaskPrefab(string elementSym)
    {
        GameObject prefab = defaultFlaskPrefab;
        for (int i = 0; i < elementKeys.Length; i++)
        {
            if (elementKeys[i] == elementSym &&
                i < flaskPrefabs.Length && flaskPrefabs[i] != null)
            {
                prefab = flaskPrefabs[i];
                break;
            }
        }
        return prefab;
    }

    private void SpawnOrUpdateOverlay(string elementSym)
    {
        if (overlayBillboardPrefab == null || overlayParent == null) return;

        // 生成（1回だけ）
        if (_activeOverlay == null)
        {
            _activeOverlay = VRCInstantiate(overlayBillboardPrefab);
            if (_activeOverlay == null) return;

            _activeOverlay.transform.SetParent(overlayParent, false);
            _activeOverlay.transform.localScale = new Vector3(overlaySize.x, overlaySize.y, 1f);
            _activeOverlayRenderer = _activeOverlay.GetComponentInChildren<Renderer>(true);
        }

        // テクスチャ差し替え（material インスタンスに対して実行）
        if (_activeOverlayRenderer != null)
        {
            Texture tex = ResolveOverlayTexture(elementSym);
            Material mat = _activeOverlayRenderer.material; // インスタンス（new はしない）
            if (mat != null && tex != null)
            {
                mat.SetTexture("_MainTex", tex);
            }
        }

        _activeOverlay.SetActive(true);
    }

    private Texture ResolveOverlayTexture(string elementSym)
    {
        for (int i = 0; i < elementKeys.Length; i++)
        {
            if (elementKeys[i] == elementSym &&
                i < elementOverlaySprites.Length &&
                elementOverlaySprites[i] != null)
            {
                return elementOverlaySprites[i].texture;
            }
        }
        return null;
    }

    private Color ResolveLiquidColor(string elementSym)
    {
        for (int i = 0; i < elementKeys.Length; i++)
        {
            if (elementKeys[i] == elementSym && i < elementLiquidColors.Length)
                return elementLiquidColors[i];
        }
        // デフォルト色
        return new Color(0.1f, 0.5f, 1f, 1f);
    }

    // WireframeFX パラメータ適用（new/Destroy 不使用）
    private void ApplyWireShaderParams(GameObject flask, string elementSym)
    {
        if (flask == null) return;

        Color col = ResolveLiquidColor(elementSym);
        Renderer[] rends = flask.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];

            // materials は実体の配列。各要素に直接 Set 系を行う（new Material しない）
            Material[] mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null) continue;

                // プロパティ存在チェックは省略（Udon互換優先）。該当しない場合は単に反映されないだけ。
                // ゲート無効化（ゾーン外で液体が消えるのを防ぐ）
                mat.SetFloat("_UseWorldZone", 0f);

                // 液体をしっかり表示
                mat.SetFloat("_LiquidAlpha", 1.0f);
                mat.SetColor("_LiquidColor", col);

                // ほぼ満タン（“あふれる”見た目）
                mat.SetFloat("_FillLevel", 0.98f);
                mat.SetFloat("_SurfaceSoft", 0.01f);

                // ワイヤ表示は任意（お好みで）
                mat.SetFloat("_WireEnable", 1f);
                mat.SetFloat("_WireFill", 1f);

                // ガラスの不透明度を少しだけ残す
                mat.SetFloat("_Opacity", 0.05f);
            }
        }
    }

    // ===== Udon制約に合わせた空実装（生成は行わない） =====
    public void AddAtom(string id, string element, int isotopeMass, int charge) { SetElementId(id, element); }
    public void SetIsotope(string id, int mass) { /* no-op */ }
    public void SetCharge(string id, int q) { /* no-op */ }
    public void AddBond(string idA, string idB, int order) { /* no-op */ }
    public void Relayout(string hint) { /* no-op */ }
}
