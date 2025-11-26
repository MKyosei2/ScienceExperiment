Shader "ChemLab/LiquidSurface"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0.5, 1, 0.7)
        _RipplePower ("Ripple Power", Float) = 0
        _PulseColor ("Pulse Color", Color) = (1,1,1,0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _RipplePower;
            float4 _PulseColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                float ripple = sin(v.vertex.x * 20 + _RipplePower * 10) * 0.002;
                o.pos = UnityObjectToClipPos(v.vertex + float4(0, ripple, 0, 0));
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = _Color;

                // Pulse effect
                col += _PulseColor * 0.5;

                return col;
            }
            ENDCG
        }
    }
}
