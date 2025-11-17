using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// 元素/器具ボタンで実験器具をスポーンし、
/// その子に付いている全ての ParticleSystem を
/// その器具のメッシュ形状に合わせて「位置・大きさ・色」を自動調整する。
/// 元の ParticleSystem の配置は一切信用せず、実験器具側の Renderer から
/// Bounds を計算して自動でフィットさせる。
/// </summary>
[AddComponentMenu("VRC Lab/ChemElementSpawner (Auto Fit Particles)")]
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
    // 元素ボタン
    // =========================================================
    public void _SelectElement()
    {
        hasElementSelected = true;
        Debug.Log("[Spawner] _SelectElement: element=" + selectedElementName);

        GameObject flask = SpawnVessel();
        if (flask == null) return;

        ApplyParticlesAutoFit(flask, selectedElementName);
    }

    // =========================================================
    // 器具ボタン
    // =========================================================
    public void _SelectEquipment()
    {
        Debug.Log("[Spawner] _SelectEquipment: equipment=" + selectedEquipmentName);

        GameObject flask = SpawnVessel();
        if (flask == null) return;

        if (hasElementSelected && !string.IsNullOrEmpty(selectedElementName))
        {
            ApplyParticlesAutoFit(flask, selectedElementName);
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
    private GameObject SpawnVessel()
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
            Debug.LogError("[Spawner] SpawnVessel に失敗しました。");
            return null;
        }

        if (spawnParent != null)
            inst.transform.SetParent(spawnParent, true);

        if (spawnedCount < spawned.Length)
            spawned[spawnedCount++] = inst;

        Debug.Log("[Spawner] SpawnVessel: " + inst.name);
        return inst;
    }

    // =========================================================
    // ★ コア：実験器具の形からパーティクル位置・形状を自動フィット ★
    // =========================================================
    private void ApplyParticlesAutoFit(GameObject root, string substanceName)
    {
        if (root == null) return;

        // --- 1) 子孫から全 Renderer を取得（ParticleSystemRenderer は除外） ---
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("[Spawner] Renderer が見つかりません: root=" + root.name);
            return;
        }

        bool boundsInit = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            if (r.GetComponent<ParticleSystemRenderer>() != null) continue; // 中身の粒は除外

            if (!boundsInit)
            {
                bounds = r.bounds;
                boundsInit = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!boundsInit)
        {
            Debug.LogWarning("[Spawner] メッシュ用 Renderer が見つかりません（全て PSRenderer？）: root=" + root.name);
            return;
        }

        // 器具全体の中心と大きさ（ワールド）
        Vector3 worldCenter = bounds.center;
        Vector3 worldExtents = bounds.extents;

        // ルートのローカル座標系に変換
        Transform rootT = root.transform;
        Vector3 localCenter = rootT.InverseTransformPoint(worldCenter);

        // 器具の横幅・高さをざっくり取得
        float radius = Mathf.Max(worldExtents.x, worldExtents.z) * 0.6f; // ちょい内側に
        float height = worldExtents.y * 1.2f;                            // 少し余裕を持たせる

        if (radius <= 0f) radius = 0.1f;
        if (height <= 0f) height = 0.2f;

        // --- 2) 子孫から全 ParticleSystem を取得 ---
        ParticleSystem[] psList = root.GetComponentsInChildren<ParticleSystem>(true);
        if (psList == null || psList.Length == 0)
        {
            Debug.LogWarning("[Spawner] ParticleSystem が見つかりません: root=" + root.name);
            return;
        }

        // --- 3) 色決定 ---
        Color col = GetColorForSubstance(substanceName);

        // --- 4) 各 ParticleSystem にフィット処理＋色適用 ---
        for (int i = 0; i < psList.Length; i++)
        {
            ParticleSystem ps = psList[i];
            if (ps == null) continue;

            GameObject go = ps.gameObject;
            if (!go.activeSelf) go.SetActive(true);

            Transform t = ps.transform;

            // 位置は器具の中心（やや下寄りにしたければ localCenter.y を少し下げる）
            t.localPosition = localCenter;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var main = ps.main;
            main.startColor = col;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            // 粒の基本パラメータ（とりあえず見えるように）
            float size = main.startSize.constant;
            if (size < 0.02f) size = 0.02f;
            main.startSize = size;

            float life = main.startLifetime.constant;
            if (life < 1f) life = 1.5f;
            main.startLifetime = life;

            main.startSpeed = 0.0f;

            var emission = ps.emission;
            emission.enabled = true;
            if (emission.rateOverTime.constant < 15f)
                emission.rateOverTime = 20f;

            // 形状モジュールを器具にフィットさせる
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone; // おおまかにフラスコ／ビーカー向け
            shape.radius = radius;
            shape.angle = 10f;
            shape.length = height;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = Vector3.one;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r != null) r.enabled = true;

            ps.Clear();
            ps.Play();
        }

        Debug.Log("[Spawner] AutoFitParticles: '" + substanceName + "' center=" + localCenter +
                  " radius=" + radius + " height=" + height + " / PS 数=" + psList.Length);
    }

    // =========================================================
    // 物質名 → 色（118元素＋未知化合物＋オーバーライド）
    // =========================================================
    private Color GetColorForSubstance(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Color.white;

        // 1. オーバーライド（Rh など）
        Color col;
        if (TryOverride(name, out col))
            return col;

        // 2. 元素記号っぽいかどうか
        bool isElement = IsElementSymbol(name);

        // 3. 名前からハッシュして色相を決定
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

    private float HashHue(string s)
    {
        int h = 0;
        for (int i = 0; i < s.Length; i++)
            h = (h * 131) ^ s[i];

        return (h & 0xFFFF) / 65535f;
    }
}
