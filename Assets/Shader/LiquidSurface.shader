Shader "ChemLab/LiquidSurface"
{
    Properties
    {
        _Color ("Liquid Color", Color) = (0,0.5,1,0.6)
        _Smoothness ("Smoothness", Range(0,1)) = 0.8
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _WaveStrength ("Wave Strength", Range(0,1)) = 0.1
        _WaveSpeed ("Wave Speed", Range(0,5)) = 1.0
    }

    SubShader
    {
        Tags {"Queue"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float _WaveStrength;
            float _WaveSpeed;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * 6.0) * _WaveStrength;
                v.vertex.y += wave;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
