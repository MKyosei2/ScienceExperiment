using UnityEngine;

[CreateAssetMenu(fileName = "ShaderEffectData", menuName = "CHEMLAB/Shader Effect Profile", order = 1)]
public class ShaderEffectData : ScriptableObject
{
    [Header("💧 Liquid")]
    public Color liquidColor = new Color(0.1f, 0.5f, 1f, 0f);
    public float liquidAlpha = 0.5f;
    public float fillLevel = 0.5f;
    public float wobble = 0.06f;

    [Header("🔻 Precipitate")]
    public Color precipitateColor = new Color(0.8f, 0.4f, 0.2f, 1f);
    public float precipitateAmount = 0.2f;

    [Header("🌀 Swirl")]
    public float swirlStrength = 0.5f;
    public float swirlSpeed = 3.0f;

    [Header("✨ Heat & Sparkle")]
    public float sparkle = 0.0f;
    public float heat = 0.0f;
}
