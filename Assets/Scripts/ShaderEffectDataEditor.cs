using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShaderEffectData))]
public class ShaderEffectDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ShaderEffectData data = (ShaderEffectData)target;

        data.effectType = (ShaderEffectType)EditorGUILayout.EnumPopup("Effect Type", data.effectType);

        data.effectColor = EditorGUILayout.ColorField("Color", data.effectColor);
        data.intensity = EditorGUILayout.FloatField("Intensity", data.intensity);

        EditorGUILayout.Space();

        switch (data.effectType)
        {
            case ShaderEffectType.Bubble:
                data.bubbleSpeed = EditorGUILayout.FloatField("Bubble Speed", data.bubbleSpeed);
                data.bubbleNoise = (Texture2D)EditorGUILayout.ObjectField("Noise Texture", data.bubbleNoise, typeof(Texture2D), false);
                break;
            case ShaderEffectType.Liquid:
                data.liquidWobbleAmount = EditorGUILayout.FloatField("Wobble Amount", data.liquidWobbleAmount);
                data.liquidTex = (Texture2D)EditorGUILayout.ObjectField("Liquid Texture", data.liquidTex, typeof(Texture2D), false);
                break;
            case ShaderEffectType.Heat:
                data.heatDistortionAmount = EditorGUILayout.FloatField("Distortion Amount", data.heatDistortionAmount);
                data.heatWaveMap = (Texture2D)EditorGUILayout.ObjectField("Heat Wave Map", data.heatWaveMap, typeof(Texture2D), false);
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }
}