using UnityEngine;

public enum ShaderEffectType { Bubble, Liquid, Heat }

[CreateAssetMenu(fileName = "NewShaderEffect", menuName = "Shader/EffectData")]
public class ShaderEffectData : ScriptableObject
{
    public ShaderEffectType effectType;

    public float bubbleSpeed = 0.5f;
    public Texture2D bubbleNoise;

    public float liquidWobbleAmount = 0.1f;
    public Texture2D liquidTex;

    public float heatDistortionAmount = 0.1f;
    public Texture2D heatWaveMap;
}
