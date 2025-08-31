using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GlassRendererController : UdonSharpBehaviour
{
    [Header("Material (assign directly)")]
    [Tooltip("対象のマテリアルを直接割り当ててください")]
    public Material targetMaterial;

    [Header("Container Meta")]
    [Tooltip("mL: 容器の公称容量")]
    public float containerCapacityML = 250f;
    [Tooltip("0..1: 開口の広さ係数（広いほど揺動が逃げる）")]
    public float mouthFactor = 0.3f;
    [Tooltip("0..1: 胴の広さ係数（広いほど波が出やすい）")]
    public float bodyFactor = 0.7f;

    [Header("Current State (primary)")]
    public Color liquidColor = new Color(0.0f, 0.6f, 1.0f, 0.85f);
    public Color precipitateColor = new Color(1f, 1f, 1f, 0.0f);

    [Tooltip("0..1")] public float viscosity = 0.5f;
    [Tooltip("0..0.5")] public float waveAmplitude = 0.05f;    // = wobble
    [Tooltip("0..1")] public float foamLevel = 0.0f;
    [Tooltip("0..1")] public float heatDistortion = 0.0f;       // = heat
    [Tooltip("0..1")] public float turbidity = 0.0f;
    [Tooltip("0..0.98")] public float fillRatio = 0.4f;         // = fillLevel

    [Header("Motion/VR")]
    [Tooltip("VRの傾き角(0..90deg)")]
    public float tiltDeg;
    [Tooltip("0..1: 外部から与える攪拌強度（負なら無効）")]
    public float stirOverride = -1f;

    // ====== 決定論レンジ（const は使用可）======
    private const float LIQUID_MIN_A = 0.0f;
    private const float LIQUID_MAX_A = 1.0f;
    private const float PRECIP_MIN_A = 0.0f;
    private const float PRECIP_MAX_A = 1.0f;
    private const float VISC_MIN = 0.0f;
    private const float VISC_MAX = 1.0f;
    private const float WAVE_MIN = 0.0f;
    private const float WAVE_MAX = 0.5f;
    private const float FOAM_MIN = 0.0f;
    private const float FOAM_MAX = 1.0f;
    private const float HEAT_MIN = 0.0f;
    private const float HEAT_MAX = 1.0f;
    private const float TURB_MIN = 0.0f;
    private const float TURB_MAX = 1.0f;
    private const float FILL_MIN = 0.0f;
    private const float FILL_MAX = 0.98f;

    // ====== Shader property names（文字列で保持）======
    [Header("Shader Property Names")]
    public string prop_LiquidColor = "_LiquidColor";
    public string prop_PrecipColor = "_PrecipColor";
    public string prop_Viscosity = "_Viscosity";
    public string prop_WaveAmplitude = "_WaveAmplitude";
    public string prop_FoamLevel = "_FoamLevel";
    public string prop_HeatDistortion = "_HeatDistortion";
    public string prop_Turbidity = "_Turbidity";
    public string prop_FillRatio = "_FillRatio";
    public string prop_StirStrength = "_StirStrength";
    // 互換系（あれば使う）
    public string prop_SwirlStrength = "_SwirlStrength";
    public string prop_SwirlSpeed = "_SwirlSpeed";
    public string prop_Sparkle = "_Sparkle";

    // ====== ResultReceiver 互換フィールド ======
    [Header("Compat Aliases (for external scripts)")]
    [Tooltip("0..1 : liquidColor.a と同期")] public float liquidAlpha = 0.85f;     // -> liquidColor.a
    [Tooltip("0..0.98 : fillRatio と同期")] public float fillLevel = 0.4f;          // -> fillRatio
    [Tooltip("0..0.5 : waveAmplitude と同期")] public float wobble = 0.05f;          // -> waveAmplitude
    [Tooltip("0..1 : precipitateColor.a と同期")] public float precipitateAmount = 0.0f; // -> precipitateColor.a
    [Tooltip("0..1 : シェーダーが持っていれば利用")] public float swirlStrength = 0.0f;
    [Tooltip("任意スケール : シェーダーが持っていれば利用")] public float swirlSpeed = 0.0f;
    [Tooltip("0..1 : シェーダーが持っていれば利用")] public float sparkle = 0.0f;
    [Tooltip("0..1 : heatDistortion と同期")] public float heat = 0.0f;             // -> heatDistortion

    void Start()
    {
        // 初期同期＋適用
        SyncCompatToPrimary();
        ApplyAllToMaterial();
    }

    // === 互換フィールド -> 本体パラメータ へ反映 ===
    private void SyncCompatToPrimary()
    {
        liquidAlpha = Mathf.Clamp(liquidAlpha, LIQUID_MIN_A, LIQUID_MAX_A);
        var lc = liquidColor; lc.a = liquidAlpha; liquidColor = lc;

        fillLevel = Mathf.Clamp(fillLevel, FILL_MIN, FILL_MAX);
        fillRatio = fillLevel;

        wobble = Mathf.Clamp(wobble, WAVE_MIN, WAVE_MAX);
        waveAmplitude = wobble;

        precipitateAmount = Mathf.Clamp(precipitateAmount, PRECIP_MIN_A, PRECIP_MAX_A);
        var pc = precipitateColor; pc.a = precipitateAmount; precipitateColor = pc;

        heat = Mathf.Clamp(heat, HEAT_MIN, HEAT_MAX);
        heatDistortion = heat;

        if (swirlStrength < 0f) swirlStrength = 0f;
        if (swirlSpeed < 0f) swirlSpeed = 0f;
        if (sparkle < 0f) sparkle = 0f;
    }

    public void ApplyAllToMaterial()
    {
        if (targetMaterial == null) return;

        // 決定論 Clamp
        var lc = liquidColor; lc.a = Mathf.Clamp(lc.a, LIQUID_MIN_A, LIQUID_MAX_A);
        var pc = precipitateColor; pc.a = Mathf.Clamp(pc.a, PRECIP_MIN_A, PRECIP_MAX_A);

        viscosity = Mathf.Clamp(viscosity, VISC_MIN, VISC_MAX);
        waveAmplitude = Mathf.Clamp(waveAmplitude, WAVE_MIN, WAVE_MAX);
        foamLevel = Mathf.Clamp(foamLevel, FOAM_MIN, FOAM_MAX);
        heatDistortion = Mathf.Clamp(heatDistortion, HEAT_MIN, HEAT_MAX);
        turbidity = Mathf.Clamp(turbidity, TURB_MIN, TURB_MAX);
        fillRatio = Mathf.Clamp(fillRatio, FILL_MIN, FILL_MAX);

        // 攪拌（傾き→攪拌）
        float stirByTilt = Mathf.Clamp01(Mathf.Sin(tiltDeg * Mathf.Deg2Rad)) * (0.6f + 0.4f * bodyFactor);
        float mouthLoss = Mathf.Lerp(1.0f, 0.7f, mouthFactor);
        float capBoost = Mathf.Clamp01(Mathf.Sqrt(containerCapacityML / 1000f));
        float stirComputed = Mathf.Clamp01(stirByTilt * mouthLoss * (0.6f + 0.4f * capBoost));
        float stir = (stirOverride >= 0f) ? Mathf.Clamp01(stirOverride) : stirComputed;

        // Set（文字列プロパティ名で直接セット）
        targetMaterial.SetColor(prop_LiquidColor, lc);
        targetMaterial.SetColor(prop_PrecipColor, pc);
        targetMaterial.SetFloat(prop_Viscosity, viscosity);
        targetMaterial.SetFloat(prop_WaveAmplitude, waveAmplitude + stir * (0.15f + 0.35f * (1f - viscosity)));
        targetMaterial.SetFloat(prop_FoamLevel, foamLevel);
        targetMaterial.SetFloat(prop_HeatDistortion, heatDistortion);
        targetMaterial.SetFloat(prop_Turbidity, Mathf.Clamp01(turbidity + foamLevel * 0.3f));
        targetMaterial.SetFloat(prop_FillRatio, fillRatio);
        targetMaterial.SetFloat(prop_StirStrength, stir);

        // 互換系（存在しないプロパティ名でも Set は無害）
        targetMaterial.SetFloat(prop_SwirlStrength, swirlStrength);
        targetMaterial.SetFloat(prop_SwirlSpeed, swirlSpeed);
        targetMaterial.SetFloat(prop_Sparkle, sparkle);

        // 互換フィールドへ書き戻し（外部監視用）
        liquidAlpha = lc.a;
        fillLevel = fillRatio;
        wobble = waveAmplitude;
        precipitateAmount = pc.a;
        heat = heatDistortion;
    }

    // ====== 互換メソッド（ResultReceiver用）======
    public void ApplyEffects()
    {
        // 外部から互換フィールドだけ更新されたケースを吸収
        SyncCompatToPrimary();
        ApplyAllToMaterial();
    }

    // ====== 既存API（保持）======
    public void SetLiquidRGBA(float r, float g, float b, float a)
    {
        liquidColor = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp(a, LIQUID_MIN_A, LIQUID_MAX_A));
        liquidAlpha = liquidColor.a; // 互換も同期
        ApplyAllToMaterial();
    }

    public void SetPrecipRGBA(float r, float g, float b, float a)
    {
        precipitateColor = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp(a, PRECIP_MIN_A, PRECIP_MAX_A));
        precipitateAmount = precipitateColor.a; // 互換も同期
        ApplyAllToMaterial();
    }

    public void SetPhysicals(float newViscosity, float newWave, float newFoam, float newHeat, float newTurbidity, float newFill)
    {
        viscosity = newViscosity;
        waveAmplitude = newWave;
        foamLevel = newFoam;
        heatDistortion = newHeat;
        turbidity = newTurbidity;
        fillRatio = newFill;

        // 互換へ同期
        wobble = waveAmplitude;
        heat = heatDistortion;
        fillLevel = fillRatio;

        ApplyAllToMaterial();
    }

    public void CleanupVisual()
    {
        liquidColor = new Color(0f, 0f, 0f, 0f);
        precipitateColor = new Color(1f, 1f, 1f, 0f);
        viscosity = 0.5f;
        waveAmplitude = 0.0f;
        foamLevel = 0.0f;
        heatDistortion = 0.0f;
        turbidity = 0.0f;
        fillRatio = 0.0f;
        stirOverride = -1f;
        tiltDeg = 0f;

        // 互換もクリア
        liquidAlpha = 0f;
        fillLevel = 0f;
        wobble = 0f;
        precipitateAmount = 0f;
        swirlStrength = 0f;
        swirlSpeed = 0f;
        sparkle = 0f;
        heat = 0f;

        ApplyAllToMaterial();
    }

    // ====== 受理レンジ問い合わせ（必要なら外部で使用）======
    public float GetMinViscosity() { return VISC_MIN; }
    public float GetMaxViscosity() { return VISC_MAX; }
    public float GetMinWaveAmp() { return WAVE_MIN; }
    public float GetMaxWaveAmp() { return WAVE_MAX; }
    public float GetMinFoam() { return FOAM_MIN; }
    public float GetMaxFoam() { return FOAM_MAX; }
    public float GetMinHeat() { return HEAT_MIN; }
    public float GetMaxHeat() { return HEAT_MAX; }
    public float GetMinTurb() { return TURB_MIN; }
    public float GetMaxTurb() { return TURB_MAX; }
    public float GetMinFill() { return FILL_MIN; }
    public float GetMaxFill() { return FILL_MAX; }
}