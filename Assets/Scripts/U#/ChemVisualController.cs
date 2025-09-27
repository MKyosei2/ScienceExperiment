using UdonSharp;
using UnityEngine;

public class ChemVisualController : UdonSharpBehaviour
{
    private Renderer _rend;
    private Material _mat; // インスタンス化（共有材を汚さない）

    void Start()
    {
        _rend = GetComponentInChildren<Renderer>();
        if (_rend == null) _rend = GetComponent<Renderer>();
        if (_rend != null) _mat = _rend.material;
    }

    public void SetElementAppearance(Color elementColor)
    {
        if (_mat == null) return;

        if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", elementColor);
        if (_mat.HasProperty("_WireColor")) _mat.SetColor("_WireColor", elementColor * 1.2f);
        if (_mat.HasProperty("_GlowIntensity")) _mat.SetFloat("_GlowIntensity", 0.8f);
        // 透明＋ワイヤのみにならないよう、必要ならフィルパス用フラグもONにする
        if (_mat.HasProperty("_FillEnabled")) _mat.SetFloat("_FillEnabled", 1f);
    }

    public void UpdateEnvironment(float temperature, float pressure)
    {
        if (_mat == null) return;

        // 温度→発光・沸騰度、圧力→色シフト（Shader側に無いなら無視されるだけ）
        if (_mat.HasProperty("_GlowIntensity"))
        {
            float glow = Mathf.Clamp01((temperature - 20f) / 80f) * 2f;
            _mat.SetFloat("_GlowIntensity", glow);
        }
        if (_mat.HasProperty("_BoilAmount"))
        {
            float boil = Mathf.Clamp01((temperature - 90f) / 20f);
            _mat.SetFloat("_BoilAmount", boil);
        }
        if (_mat.HasProperty("_Evaporation"))
        {
            float evap = Mathf.Clamp01((temperature - 60f) / 40f);
            _mat.SetFloat("_Evaporation", evap);
        }
        if (_mat.HasProperty("_ColorShift"))
        {
            float pressureShift = Mathf.Clamp(pressure / 5f, -1f, 1f);
            _mat.SetFloat("_ColorShift", pressureShift);
        }
    }
}
