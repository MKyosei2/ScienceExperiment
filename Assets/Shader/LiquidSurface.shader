Shader "VRC_ChemLab/LiquidSurface"
{
    Properties
    {
        _Color("Color", Color) = (0.5, 0.5, 1, 0.8)
        _RippleStrength("Ripple Strength", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;
            float _RippleStrength;
            float4 _Time;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float ripple = sin((v.vertex.x + v.vertex.z + _Time.y * 4) * 6) * _RippleStrength * 0.04;
                v.vertex.y += ripple;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
