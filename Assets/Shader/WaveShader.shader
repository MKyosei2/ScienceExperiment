Shader "VRC_ChemLab/WaveEffect"
{
    Properties
    {
        _Color("Color", Color) = (0.6,0.6,1,0.5)
        _WaveStrength("Wave Strength", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // UnityObjectToClipPos is declared in UnityCG.cginc.
            #include "UnityCG.cginc"

            float4 _Color;
            float _WaveStrength;
            float4 _Time;

            struct appdata { float4 v:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata i)
            {
                v2f o;
                float wave = sin((i.v.x*8 + i.v.z*8 + _Time.y*10)) * _WaveStrength * 0.03;
                i.v.y += wave;
                o.p = UnityObjectToClipPos(i.v);
                o.uv = i.uv;
                return o;
            }

            float4 frag(v2f i):SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
