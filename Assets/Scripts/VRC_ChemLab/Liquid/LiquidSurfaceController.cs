using UdonSharp;
using UnityEngine;

public class LiquidSurfaceController : UdonSharpBehaviour
{
    [Header("Renderer")]
    public MeshRenderer surfaceRenderer;       // LiquidSurface の MeshRenderer

    [Header("Gas Effect")]
    public ParticleSystem gasParticle;         // 気体用粒子（任意）

    // 内部状態（必要に応じスパナーが書き換える）
    public float waveStrength = 0f;
    public float viscosity = 1f;
    public float rippleStrength = 0f;

    // キャッシュ
    private Material surfaceMat;
    private ParticleSystem.EmissionModule gasEmission;

    private const int STATE_SOLID = 0;
    private const int STATE_LIQUID = 1;
    private const int STATE_GAS = 2;

    private void Start()
    {
        if (surfaceRenderer != null)
            surfaceMat = surfaceRenderer.material;

        if (gasParticle != null)
            gasEmission = gasParticle.emission;
    }

    // ============================================================
    // 外部API（ChemElementSpawner から呼ばれる）
    // ============================================================
    public void SetSurfaceState(Color elementColor, int state)
    {
        if (surfaceMat == null) return;

        switch (state)
        {
            case STATE_SOLID:
                ApplySolid(elementColor);
                break;

            case STATE_LIQUID:
                ApplyLiquid(elementColor);
                break;

            case STATE_GAS:
                ApplyGas(elementColor);
                break;
        }
    }

    // 波 / 粘度 / Ripple（他スクリプトが呼ぶ可能性）
    public void SetWave(float v) { waveStrength = v; }
    public void SetViscosity(float v) { viscosity = v; }
    public void SetRipple(float v) { rippleStrength = v; }

    public void PulseColor(Color c)
    {
        if (surfaceMat != null)
            surfaceMat.color = c;
    }

    // ============================================================
    // Solid（固体）
    // ============================================================
    private void ApplySolid(Color col)
    {
        // 固体 → 表示はするが、色を暗く
        if (surfaceMat != null)
        {
            surfaceMat.color = new Color(col.r * 0.6f, col.g * 0.6f, col.b * 0.6f, 1f);
        }

        if (surfaceRenderer != null)
            surfaceRenderer.enabled = true;

        if (gasParticle != null)
        {
            gasEmission.enabled = false;
        }
    }

    // ============================================================
    // Liquid（液体）
    // ============================================================
    private void ApplyLiquid(Color col)
    {
        if (surfaceMat != null)
        {
            surfaceMat.color = col;
        }

        if (surfaceRenderer != null)
            surfaceRenderer.enabled = true;

        if (gasParticle != null)
        {
            gasEmission.enabled = false;
        }
    }

    // ============================================================
    // Gas（気体）
    // ============================================================
    private void ApplyGas(Color col)
    {
        // 液体は透明にする
        if (surfaceMat != null)
        {
            surfaceMat.color = new Color(col.r, col.g, col.b, 0f);
        }

        // メッシュ非表示
        if (surfaceRenderer != null)
            surfaceRenderer.enabled = false;

        // ガス粒子表示
        if (gasParticle != null)
        {
            gasEmission.enabled = true;
            gasEmission.rateOverTime = 3f;
        }
    }
}
