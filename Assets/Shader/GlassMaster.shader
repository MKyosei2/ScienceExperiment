Shader "Custom/GlassMaster"
{
    Properties
    {
        _MainColor ("Base Color", Color) = (1,1,1,0.2)
        _BubbleSpeed ("Bubble Speed", Float) = 0.5
        _BubbleNoise ("Bubble Noise", 2D) = "white" {}
        _LiquidTex ("Liquid Tex", 2D) = "white" {}
        _WobbleAmount ("Wobble", Float) = 0.1
        _HeatWaveMap ("Heat Wave", 2D) = "white" {}
        _HeatDistortion ("Heat Distortion", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
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
                float2 uv : TEXCOORD0;
            };

            sampler2D _BubbleNoise;
            float _BubbleSpeed;
            sampler2D _LiquidTex;
            float _WobbleAmount;
            sampler2D _HeatWaveMap;
            float _HeatDistortion;

            fixed4 _MainColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Bubble Effect
                float2 bubbleUV = uv + float2(0, _Time.y * _BubbleSpeed);
                float bubble = tex2D(_BubbleNoise, bubbleUV).r;

                // Liquid Wobble Effect
                float liquid = tex2D(_LiquidTex, uv + sin(_Time.y + uv.yx * 10.0) * _WobbleAmount).r;

                // Heat Distortion
                float heat = tex2D(_HeatWaveMap, uv + sin(_Time.y + uv * 20.0) * _HeatDistortion).r;

                float finalAlpha = _MainColor.a + bubble * 0.2 + liquid * 0.2 + heat * 0.2;
                return fixed4(_MainColor.rgb, saturate(finalAlpha));
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
