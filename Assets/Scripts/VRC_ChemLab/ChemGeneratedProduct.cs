using UdonSharp;
using UnityEngine;

/// <summary>
/// ChemGeneratedProduct
/// ・生成AIから渡された「個体差パラメータ」を見た目に反映
/// ・このPrefabが存在する限り値は固定
/// </summary>
public class ChemGeneratedProduct : UdonSharpBehaviour
{
    [Header("Visual Targets")]
    public Transform visualRoot;          // スケール変更対象
    public Renderer targetRenderer;       // マテリアル変更対象
    public Light optionalGlowLight;        // 発光表現（任意）

    // ===============================
    // 個体差パラメータ（固定）
    // ===============================
    [Header("Individual Parameters (Fixed)")]
    [Range(0.5f, 1.5f)] public float sizeFactor = 1f;
    [Range(-0.2f, 0.2f)] public float colorShift = 0f;
    [Range(0f, 1f)] public float roughness = 0.5f;
    [Range(0f, 1f)] public float glowIntensity = 0f;
    [Range(0f, 1f)] public float stability = 1f;

    private bool _applied;

    /// <summary>
    /// AIから呼ばれる（1回だけ）
    /// </summary>
    public void ApplyParameters(
        float size,
        float color,
        float rough,
        float glow,
        float stab
    )
    {
        if (_applied) return;
        _applied = true;

        sizeFactor = size;
        colorShift = color;
        roughness = rough;
        glowIntensity = glow;
        stability = stab;

        ApplyVisuals();
    }

    // ===============================
    // 見た目反映
    // ===============================
    private void ApplyVisuals()
    {
        // --- サイズ ---
        if (visualRoot != null)
        {
            visualRoot.localScale *= sizeFactor;
        }

        // --- マテリアル ---
        if (targetRenderer != null)
        {
            Material mat = targetRenderer.material;

            // 色ずらし（HSV的な簡易処理）
            Color c = mat.color;
            c.r = Mathf.Clamp01(c.r + colorShift);
            c.g = Mathf.Clamp01(c.g - colorShift * 0.5f);
            c.b = Mathf.Clamp01(c.b + colorShift * 0.3f);
            mat.color = c;

            // ラフネス（Standard想定）
            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", Mathf.Clamp01(1f - roughness));
            }
        }

        // --- 発光 ---
        if (optionalGlowLight != null)
        {
            optionalGlowLight.intensity = glowIntensity * 3f;
            optionalGlowLight.enabled = glowIntensity > 0.05f;
        }
    }
}
