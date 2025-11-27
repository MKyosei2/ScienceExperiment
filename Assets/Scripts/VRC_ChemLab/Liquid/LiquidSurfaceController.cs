using UdonSharp;
using UnityEngine;

public class LiquidSurfaceController : UdonSharpBehaviour
{
    public MeshRenderer surfaceRenderer;

    // Ripple
    private float rippleTimer = 0f;
    private float rippleDuration = 0f;
    private float ripplePower = 0f;

    // Glow / PulseColor
    private float glowTimer = 0f;
    private float glowDuration = 0f;
    private float glowPower = 0f;
    private Color pulseColor = Color.clear;

    // Wave
    private float waveLevel = 0f;
    private float viscosity = 1f;

    // Tilt normal
    private Vector3 liquidNormal = Vector3.up;

    private void Start()
    {
        if (surfaceRenderer == null)
            surfaceRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        Material m = surfaceRenderer.material;

        // ---------- Ripple ----------
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

        // ---------- Glow Pulse ----------
        if (glowTimer > 0f)
        {
            glowTimer -= Time.deltaTime;
            float normalized = glowTimer / glowDuration;
            m.SetFloat("_Glow", glowPower * normalized);
            m.SetColor("_PulseColor", pulseColor * normalized);
        }
        else
        {
            m.SetFloat("_Glow", 0f);
            m.SetColor("_PulseColor", Color.clear);
        }

        // ---------- Continuous props ----------
        m.SetFloat("_WaveLevel", waveLevel);
        m.SetFloat("_Viscosity", viscosity);
        m.SetVector("_LiquidNormal", liquidNormal);
    }

    // ------------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------------

    // Ripple (波紋)
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

    // Wave
    public void SetWave(float level)
    {
        waveLevel = level;
    }

    // Viscosity
    public void SetViscosity(float v)
    {
        viscosity = Mathf.Clamp(v, 0.05f, 10f);
    }

    public void SetColor(Color c)
    {
        surfaceRenderer.material.SetColor("_Color", c);
    }

    // Tilt (液体の傾き)
    public void ApplyTilt(Quaternion flaskRot)
    {
        liquidNormal = flaskRot * Vector3.up;
    }

    // ------------------------------------------------------------------------
    // ★ PulseColor（復活！） LiquidReactionAnimator / LiquidBoilingController 用
    // ------------------------------------------------------------------------

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
        pulseColor = color;
        glowPower = power;
        glowDuration = duration;
        glowTimer = duration;
    }
}
