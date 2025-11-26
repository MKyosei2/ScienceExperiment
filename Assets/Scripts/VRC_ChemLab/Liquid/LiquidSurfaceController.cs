using UdonSharp;
using UnityEngine;

public class LiquidSurfaceController : UdonSharpBehaviour
{
    public MeshRenderer surfaceRenderer;

    // Ripple
    private float rippleTimer = 0f;
    private float rippleDuration = 0f;
    private float ripplePower = 0f;

    // Glow pulse
    private float glowTimer = 0f;
    private float glowDuration = 0f;
    private float glowPower = 0f;

    private float waveLevel = 0f;
    private float viscosity = 1f;

    private void Start()
    {
        if (surfaceRenderer == null)
            surfaceRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        Material m = surfaceRenderer.material;

        // ---------- Ripple decay ----------
        if (rippleTimer > 0f)
        {
            rippleTimer -= Time.deltaTime;
            float normalized = rippleTimer / rippleDuration;
            m.SetFloat("_RipplePower", ripplePower * normalized);
        }
        else
        {
            m.SetFloat("_RipplePower", 0f);
        }

        // ---------- Glow decay ----------
        if (glowTimer > 0f)
        {
            glowTimer -= Time.deltaTime;
            float normalized = glowTimer / glowDuration;
            m.SetFloat("_Glow", glowPower * normalized);
        }
        else
        {
            m.SetFloat("_Glow", 0f);
        }

        // ---------- Continuous properties ----------
        m.SetFloat("_WaveLevel", waveLevel);
        m.SetFloat("_Viscosity", viscosity);
    }

    // ============================================================
    // PUBLIC API - All overloads (for compatibility)
    // ============================================================

    // --- SetRipple (波紋) ---
    public void SetRipple()
    {
        SetRipple(0.2f, 0.4f);
    }

    public void SetRipple(float power)
    {
        SetRipple(power, 0.4f);
    }

    public void SetRipple(float power, float duration)
    {
        ripplePower = power;
        rippleDuration = duration;
        rippleTimer = duration;
    }

    // --- Pulse (旧API互換) ---
    public void Pulse()
    {
        SetRipple(0.2f, 0.4f);
    }

    public void Pulse(float power)
    {
        SetRipple(power, 0.4f);
    }

    public void Pulse(float power, float duration)
    {
        SetRipple(power, duration);
    }

    // --- PulseColor（発光） ---
    public void PulseColor(Color color)
    {
        PulseColor(color, 0.5f, 0.5f);
    }

    public void PulseColor(Color color, float power)
    {
        PulseColor(color, power, 0.5f);
    }

    public void PulseColor(Color color, float power, float duration)
    {
        Material m = surfaceRenderer.material;
        m.SetColor("_Color", color);

        glowPower = power;
        glowDuration = duration;
        glowTimer = duration;
    }

    // --- 波（内部揺れ） ---
    public void SetWave(float level)
    {
        waveLevel = level;
    }

    // --- 粘度設定 ---
    public void SetViscosity(float v)
    {
        viscosity = Mathf.Clamp(v, 0.05f, 10f);
    }

    // --- 色設定 ---
    public void SetColor(Color c)
    {
        surfaceRenderer.material.SetColor("_Color", c);
    }
}
