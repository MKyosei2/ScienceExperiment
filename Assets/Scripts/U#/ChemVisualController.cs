// ChemVisualController.cs
// CONICAL_FLASK に付与。環境フィールドを配列で管理し、シェーダに _Phase/_TempC/_Humidity/_PressureAtm/_ElementIndex を渡す。
// U#制約回避：List.Contains/Remove未使用、NaNフラグ未使用、全てfloat/配列で実装。

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif

#if CHEM_RUNTIME
public class ChemVisualController : UdonSharpBehaviour
#else
public class ChemVisualController : MonoBehaviour
#endif
{
    [Header("参照")]
    public ChemEnvironmentManager env;      // シーンに1つ配置し割当て

    [Header("元素記号")]
    public string elementId = "H";

    [Header("シェーダ更新対象（液体/ガラス/泡など）")]
    public Renderer[] targetRenderers;

    [Header("シェーダ・プロパティ名")]
    public string propPhase = "_Phase";              // int 0=Solid,1=Liquid,2=Gas
    public string propTempC = "_TempC";              // float
    public string propHumidity = "_Humidity";        // float
    public string propPressureAtm = "_PressureAtm";  // float (大気+水圧)
    public string propElementIndex = "_ElementIndex";// int

    [Header("重なれるEnvironmentFieldの最大数")]
    public int maxOverlappingFields = 32;

    // ---------- 内部状態 ----------
    private MaterialPropertyBlock _mpb;
    private int _elementIndex = 0;

    // U#で安全な固定長配列管理
    private EnvironmentField[] _fields;
    private int _fieldCount = 0;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _fields = new EnvironmentField[maxOverlappingFields > 0 ? maxOverlappingFields : 8];
        _elementIndex = GetElementIndex(NormalizeSymbol(elementId));
    }

    private void OnEnable()
    {
        if (env != null) env.Register(this);
        ApplyToShaders();
    }

    private void OnDisable()
    {
        if (env != null) env.Unregister(this);
    }

    private void OnDestroy()
    {
        if (env != null) env.Unregister(this);
    }

    // ---------- Triggerでフィールド出入り検知（どちらかにRigidbody必要） ----------
    private void OnTriggerEnter(Collider other)
    {
        var f = other.GetComponent<EnvironmentField>();
        if (f != null) AddFieldIfNotPresent(f);
    }

    private void OnTriggerExit(Collider other)
    {
        var f = other.GetComponent<EnvironmentField>();
        if (f != null) RemoveFieldIfPresent(f);
    }

    // 安全な配列操作（Contains/Remove相当）
    private int IndexOfField(EnvironmentField f)
    {
        for (int i = 0; i < _fieldCount; i++)
        {
            if (_fields[i] == f) return i;
        }
        return -1;
    }

    private void AddFieldIfNotPresent(EnvironmentField f)
    {
        if (f == null) return;
        int idx = IndexOfField(f);
        if (idx >= 0) return; // すでに入っている

        if (_fieldCount < _fields.Length)
        {
            _fields[_fieldCount] = f;
            _fieldCount++;
            ApplyToShaders();
        }
        // ※満杯なら無視（必要なら maxOverlappingFields を増やしてください）
    }

    private void RemoveFieldIfPresent(EnvironmentField f)
    {
        int idx = IndexOfField(f);
        if (idx < 0) return;

        // 後ろ詰め
        int last = _fieldCount - 1;
        _fields[idx] = _fields[last];
        _fields[last] = null;
        _fieldCount = last;
        ApplyToShaders();
    }

    // ---------- スポーン直後に呼ぶ ----------
    public void SetElementId(string symbol)
    {
        elementId = NormalizeSymbol(symbol);
        _elementIndex = GetElementIndex(elementId);
        ApplyToShaders();
    }

    // ---------- メイン：シェーダ反映 ----------
    public void ApplyToShaders()
    {
        if (env == null) return;

        // 1) ベース環境
        float tempC = env.baseTempC;
        float humidity = env.baseHumidity;
        float patm = env.basePressureAtm;

        // 2) フィールド重ねがけ（加算/乗算/上書き + 水圧）
        float addT = 0f, mulT = 1f, setT = 0f; bool setTHas = false;
        float addH = 0f, mulH = 1f, setH = 0f; bool setHHas = false;
        float addP = 0f, mulP = 1f, setP = 0f; bool setPHas = false;
        float waterAtm = 0f;

        for (int i = 0; i < _fieldCount; i++)
        {
            var f = _fields[i];
            if (f == null || !f.enabled) continue;

            // 温度
            if (f.tempMode == EnvBlend.Add) addT += f.tempValue;
            else if (f.tempMode == EnvBlend.Multiply) mulT *= f.tempValue;
            else /*Set*/                               { setT = f.tempValue; setTHas = true; }

            // 湿度
            if (f.humidityMode == EnvBlend.Add) addH += f.humidityValue;
            else if (f.humidityMode == EnvBlend.Multiply) mulH *= f.humidityValue;
            else /*Set*/                                   { setH = f.humidityValue; setHHas = true; }

            // 大気圧
            if (f.pressureMode == EnvBlend.Add) addP += f.pressureAtmValue;
            else if (f.pressureMode == EnvBlend.Multiply) mulP *= f.pressureAtmValue;
            else /*Set*/                                   { setP = f.pressureAtmValue; setPHas = true; }

            // 水圧
            if (f.isWaterVolume)
            {
                float depth = f.ComputeDepthAtWorldPos(transform.position);
                if (depth > 0f) waterAtm += f.HydrostaticAtm(depth);
            }
        }

        tempC = setTHas ? setT : (tempC * mulT + addT);
        humidity = setHHas ? setH : (humidity * mulH + addH);
        patm = setPHas ? setP : (patm * mulP + addP);
        patm += waterAtm; // 大気に水圧を重畳

        // 3) 相を決定（圧力依存の簡易補正）
        string sym = NormalizeSymbol(elementId);
        int phase = ResolvePhaseAtTemperature(sym, tempC, patm);

        // 4) シェーダへ
        if (targetRenderers == null || targetRenderers.Length == 0) return;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            if (!string.IsNullOrEmpty(propPhase)) _mpb.SetInt(propPhase, phase);
            if (!string.IsNullOrEmpty(propTempC)) _mpb.SetFloat(propTempC, tempC);
            if (!string.IsNullOrEmpty(propHumidity)) _mpb.SetFloat(propHumidity, humidity);
            if (!string.IsNullOrEmpty(propPressureAtm)) _mpb.SetFloat(propPressureAtm, patm);
            if (!string.IsNullOrEmpty(propElementIndex)) _mpb.SetInt(propElementIndex, _elementIndex);
            r.SetPropertyBlock(_mpb);
        }
    }

    // ---- 相判定（0:Solid / 1:Liquid / 2:Gas） ※簡易：mp/bp と温度、圧力はbpスケール補正のみ ----
    private int ResolvePhaseAtTemperature(string symbol, float tempC, float pressureAtm)
    {
        float mp, bp;
        if (!TryGetMPBP(symbol, out mp, out bp)) return 0;

        // 圧力による沸点スケール（演出優先）
        float p = pressureAtm; if (p < 0.1f) p = 0.1f;
        // Mathf.Log10 は U# で使用可。極端な値も扱えるよう下限/上限を軽くクランプ
        float scale = Mathf.Log10(p + 1f) + 1f;
        if (scale < 0.1f) scale = 0.1f; else if (scale > 100f) scale = 100f;
        float bpAdj = bp * scale;

        if (tempC < mp) return 0;
        if (tempC >= bpAdj) return 2;
        return 1;
    }

    // ---- 元素 → インデックス（シェーダ側テーブル参照用：必要分のみ）----
    private int GetElementIndex(string symbol)
    {
        switch (symbol)
        {
            case "H": return 0;
            case "He": return 1;
            case "Li": return 2;
            case "Be": return 3;
            case "B": return 4;
            case "C": return 5;
            case "N": return 6;
            case "O": return 7;
            case "F": return 8;
            case "Ne": return 9;
            case "Na": return 10;
            case "Mg": return 11;
            case "Al": return 12;
            case "Si": return 13;
            case "P": return 14;
            case "S": return 15;
            case "Cl": return 16;
            case "Ar": return 17;
            case "K": return 18;
            case "Ca": return 19;
            case "Fe": return 25;
            case "Cu": return 28;
            case "Zn": return 29;
            case "Ga": return 30;
            case "Ge": return 31;
            case "As": return 32;
            case "Se": return 33;
            case "Br": return 34;
            case "Kr": return 35;
            case "Rb": return 36;
            case "Sr": return 37;
            case "Ag": return 47;
            case "Cd": return 48;
            case "Sn": return 50;
            case "I": return 53;
            case "Xe": return 54;
            case "Cs": return 55;
            case "Ba": return 56;
            case "Pt": return 78;
            case "Au": return 79;
            case "Hg": return 80;
            case "Pb": return 82;
            case "Bi": return 83;
            case "Rn": return 86;
            default: return 0;
        }
    }

    // ---- 融点・沸点（℃）※必要分のみ。未収録は固体扱いへ ----
    private bool TryGetMPBP(string symbol, out float mpC, out float bpC)
    {
        switch (symbol)
        {
            // 1〜18
            case "H": mpC = -259.1f; bpC = -252.9f; return true;
            case "He": mpC = -272.2f; bpC = -268.9f; return true;
            case "Li": mpC = 180.5f; bpC = 1342f; return true;
            case "Be": mpC = 1287f; bpC = 2469f; return true;
            case "B": mpC = 2075f; bpC = 4000f; return true;
            case "C": mpC = 3550f; bpC = 4827f; return true;
            case "N": mpC = -210f; bpC = -195.8f; return true;
            case "O": mpC = -218.8f; bpC = -182.9f; return true;
            case "F": mpC = -219.6f; bpC = -188.1f; return true;
            case "Ne": mpC = -248.6f; bpC = -246.1f; return true;
            case "Na": mpC = 97.8f; bpC = 883f; return true;
            case "Mg": mpC = 650f; bpC = 1091f; return true;
            case "Al": mpC = 660.3f; bpC = 2470f; return true;
            case "Si": mpC = 1414f; bpC = 3265f; return true;
            case "P": mpC = 44.2f; bpC = 280.5f; return true;
            case "S": mpC = 115.2f; bpC = 444.6f; return true;
            case "Cl": mpC = -101.5f; bpC = -34.0f; return true;
            case "Ar": mpC = -189.3f; bpC = -185.8f; return true;

            // 19〜36（抜粋）
            case "K": mpC = 63.5f; bpC = 759f; return true;
            case "Ca": mpC = 842f; bpC = 1484f; return true;
            case "Fe": mpC = 1538f; bpC = 2862f; return true;
            case "Co": mpC = 1495f; bpC = 2927f; return true;
            case "Ni": mpC = 1455f; bpC = 2913f; return true;
            case "Cu": mpC = 1084.6f; bpC = 2562f; return true;
            case "Zn": mpC = 419.5f; bpC = 907f; return true;
            case "Ga": mpC = 29.8f; bpC = 2403f; return true;
            case "Ge": mpC = 938.3f; bpC = 2833f; return true;
            case "As": mpC = 817f; bpC = 614f; return true;
            case "Se": mpC = 221f; bpC = 685f; return true;
            case "Br": mpC = -7.2f; bpC = 58.8f; return true;
            case "Kr": mpC = -157.4f; bpC = -153.4f; return true;

            // 37〜56（抜粋）
            case "Rb": mpC = 39.3f; bpC = 688f; return true;
            case "Sr": mpC = 777f; bpC = 1382f; return true;
            case "Ag": mpC = 961.8f; bpC = 2162f; return true;
            case "Cd": mpC = 321.1f; bpC = 767f; return true;
            case "Sn": mpC = 231.9f; bpC = 2602f; return true;
            case "I": mpC = 113.7f; bpC = 184.3f; return true;
            case "Xe": mpC = -111.8f; bpC = -108.1f; return true;
            case "Cs": mpC = 28.4f; bpC = 671f; return true;
            case "Ba": mpC = 727f; bpC = 1897f; return true;

            // 78〜86（抜粋）
            case "Pt": mpC = 1768f; bpC = 3825f; return true;
            case "Au": mpC = 1064f; bpC = 2966f; return true;
            case "Hg": mpC = -38.8f; bpC = 356.7f; return true;
            case "Pb": mpC = 327.5f; bpC = 1749f; return true;
            case "Bi": mpC = 271.4f; bpC = 1560f; return true;
            case "Rn": mpC = -71.0f; bpC = -61.7f; return true;

            default:
                mpC = 0f; bpC = 1e9f; return false; // 未収録は固体扱いへ
        }
    }

    private static string NormalizeSymbol(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        raw = raw.Trim();
        if (raw.Length == 1) return raw.ToUpperInvariant();
        return char.ToUpperInvariant(raw[0]) + raw.Substring(1).ToLowerInvariant();
    }
}
