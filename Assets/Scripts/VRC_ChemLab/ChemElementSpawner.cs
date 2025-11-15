using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// 元素/器具ボタンでフラスコ等をスポーンし、
/// その子に付いている全ての ParticleSystem を有効化＆色変更する汎用スクリプト。
/// 階層名やオブジェクト名に依存しないよう、子孫を総当たりで処理する。
/// </summary>
[AddComponentMenu("VRC Lab/ChemElementSpawner (Particle Auto Simple)")]
public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("=== 生成先 ===")]
    [Tooltip("生成した器具をぶら下げる親 (例: Systems/Spawner)")]
    public Transform spawnParent;

    [Header("=== 器具本体 ===")]
    [Tooltip("Tool/CONICAL_FLASK など、元になる器具オブジェクト（子に Particle System が入っているもの）")]
    public GameObject sourceVessel;

    [Header("=== 色の汎用設定 ===")]
    [Tooltip("元素(H, He, Li...)の明るさ")]
    [Range(0f, 1f)] public float elementValue = 0.9f;

    [Tooltip("化合物(H2O, NaCl...)の明るさ")]
    [Range(0f, 1f)] public float compoundValue = 0.85f;

    [Tooltip("パーティクル色の彩度")]
    [Range(0f, 1f)] public float colorSaturation = 0.8f;

    [Header("=== 個別オーバーライド（任意） ===")]
    [Tooltip("特別な色を付けたい物質名（Rh, Au, H2O など）")]
    public string[] overrideNames;
    [Tooltip("overrideNames と同じ長さの色配列")]
    public Color[] overrideColors;

    [Header("=== 反応再生（既存） ===")]
    public JsonReactionPlayer reactionPlayer;

    // ==== 他スクリプトから参照されるフィールド（互換用） ====
    [HideInInspector] public string selectedEquipmentName = "";
    [HideInInspector] public string selectedElementName = "";
    [HideInInspector] public string bondData = "";

    private GameObject[] spawned = new GameObject[256];
    private int spawnedCount = 0;
    private bool hasElementSelected = false;

    // ---------- ラッパー（既存スクリプト用） ----------
    public void StartExperiment() { SendCustomEvent("_StartExperiment"); }
    public void ResetExperiment() { SendCustomEvent("_ResetExperiment"); }
    public void SelectElement(string n) { selectedElementName = n; SendCustomEvent("_SelectElement"); }
    public void SelectEquipment(string n) { selectedEquipmentName = n; SendCustomEvent("_SelectEquipment"); }
    public void ApplyBondUpdate() { SendCustomEvent("_ApplyBondUpdate"); }

    // =========================================================
    // 元素ボタン（UI から呼ばれる）
    // =========================================================
    public void _SelectElement()
    {
        hasElementSelected = true;
        Debug.Log("[Spawner] _SelectElement: element=" + selectedElementName);

        GameObject flask = SpawnFlask();
        if (flask == null) return;

        ApplyParticlesToAllChildren(flask, selectedElementName);
    }

    // =========================================================
    // 器具ボタン（UI から呼ばれる）
    // =========================================================
    public void _SelectEquipment()
    {
        Debug.Log("[Spawner] _SelectEquipment: equipment=" + selectedEquipmentName);

        GameObject flask = SpawnFlask();
        if (flask == null) return;

        if (hasElementSelected && !string.IsNullOrEmpty(selectedElementName))
        {
            ApplyParticlesToAllChildren(flask, selectedElementName);
        }
    }

    // =========================================================
    // 実験開始 / AI 更新 / リセット
    // =========================================================
    public void _StartExperiment()
    {
        Debug.Log("[Spawner] _StartExperiment");
        if (reactionPlayer != null && !string.IsNullOrEmpty(bondData))
            reactionPlayer.Play(bondData);
    }

    public void _ApplyBondUpdate()
    {
        Debug.Log("[Spawner] _ApplyBondUpdate: " + bondData);
        if (reactionPlayer != null && !string.IsNullOrEmpty(bondData))
            reactionPlayer.Play(bondData);
    }

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
    }

    // =========================================================
    // 器具本体をスポーン
    // =========================================================
    private GameObject SpawnFlask()
    {
        if (sourceVessel == null)
        {
            Debug.LogError("[Spawner] sourceVessel が未設定です。");
            return null;
        }

        GameObject inst = VRCInstantiate(sourceVessel);
        if (inst == null)
        {
            inst = Object.Instantiate(sourceVessel);
            Debug.LogWarning("[Spawner] VRCInstantiate が null → Instantiate で代用");
        }
        if (inst == null)
        {
            Debug.LogError("[Spawner] SpawnFlask に失敗しました。");
            return null;
        }

        if (spawnParent != null)
            inst.transform.SetParent(spawnParent, true);

        if (spawnedCount < spawned.Length)
            spawned[spawnedCount++] = inst;

        Debug.Log("[Spawner] SpawnFlask: " + inst.name);
        return inst;
    }

    // =========================================================
    // ★ コア：生成したフラスコの子孫の全 ParticleSystem を
    //    強制的に Active & Play し、物質名に応じた色を付ける
    // =========================================================
    private void ApplyParticlesToAllChildren(GameObject root, string substanceName)
    {
        if (root == null) return;

        // 1) 子孫から全 ParticleSystem を取得（非アクティブも対象）
        ParticleSystem[] psList = root.GetComponentsInChildren<ParticleSystem>(true);
        if (psList == null || psList.Length == 0)
        {
            Debug.LogWarning("[Spawner] ParticleSystem が見つかりません: root=" + root.name);
            return;
        }

        // 2) 物質名から色を決める（118元素＋未知化合物対応）
        Color col = GetColorForSubstance(substanceName);

        // 3) 各パーティクルに適用＆強制 ON
        for (int i = 0; i < psList.Length; i++)
        {
            ParticleSystem ps = psList[i];
            if (ps == null) continue;

            // GameObject が非アクティブなら、強制的に ON
            GameObject go = ps.gameObject;
            if (!go.activeSelf || !go.activeInHierarchy)
                go.SetActive(true);

            var main = ps.main;
            main.startColor = col;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;

            // Renderer も確実に ON
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r != null) r.enabled = true;

            ps.Clear();
            ps.Play();
        }

        Debug.Log("[Spawner] ApplyParticlesToAllChildren: '" + substanceName + "' → 色 " + col + " / PS 数=" + psList.Length);
    }

    // =========================================================
    // 物質名 → 色（118元素＋未知化合物＋オーバーライド）
    // =========================================================
    private Color GetColorForSubstance(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Color.white;

        // 1. オーバーライド（Rh など手で指定したい場合）
        Color col;
        if (TryOverride(name, out col))
            return col;

        // 2. 元素記号っぽいかどうか
        bool isElement = IsElementSymbol(name);

        // 3. 名前からハッシュして色相を決定（未知化合物でも安定）
        float hue = HashHue(name);
        float val = isElement ? elementValue : compoundValue;

        return Color.HSVToRGB(hue, colorSaturation, val);
    }

    private bool TryOverride(string name, out Color col)
    {
        col = Color.white;

        if (overrideNames == null || overrideColors == null)
            return false;

        int len = overrideNames.Length;
        if (overrideColors.Length < len)
            len = overrideColors.Length;

        for (int i = 0; i < len; i++)
        {
            if (overrideNames[i] == name)
            {
                col = overrideColors[i];
                return true;
            }
        }
        return false;
    }

    // 「1〜3文字・先頭大文字・残り小文字」のものを元素記号とみなす
    private bool IsElementSymbol(string name)
    {
        int len = name.Length;
        if (len < 1 || len > 3) return false;
        if (!char.IsUpper(name[0])) return false;

        for (int i = 1; i < len; i++)
        {
            if (!char.IsLower(name[i])) return false;
        }
        return true;
    }

    // 名前から 0〜1 の Hue を作る（同じ名前なら常に同じ色）
    private float HashHue(string s)
    {
        int h = 0;
        for (int i = 0; i < s.Length; i++)
            h = (h * 131) ^ s[i];

        return (h & 0xFFFF) / 65535f;
    }
}
