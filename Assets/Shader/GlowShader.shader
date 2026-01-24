Shader "ChemLab/GlowShader"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.2, 0.6, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0,10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            // UnityObjectToClipPos is declared in UnityCG.cginc.
            // Without this include the shader fails to compile and the effect becomes invisible/pink.
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _Color;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return _Color * _GlowIntensity;
            }
            ENDCG
        }
    }
}
