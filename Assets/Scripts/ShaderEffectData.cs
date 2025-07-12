using UnityEngine;

public enum ShaderEffectType { Bubble, Liquid, Heat }

[CreateAssetMenu(fileName = "ShaderEffectData", menuName = "ChemLab/Shader Effect")]
public class ShaderEffectData : ScriptableObject
{
    public ShaderEffectType effectType;

    // 共通パラメータ
    public Color effectColor = Color.white;
    public float intensity = 1f;

    // Bubble用
    public float bubbleSpeed;
    public Texture2D bubbleNoise;

    // Liquid用
    public float liquidWobbleAmount;
    public Texture2D liquidTex;

    // Heat用
    public float heatDistortionAmount;
    public Texture2D heatWaveMap;
}
