Shader "Custom/GlassMaster"
{
    Properties
    {
        _MainColor ("Base Color", Color) = (0.2, 0.8, 1.0, 0.8)
        _BubbleSpeed ("Bubble Speed", Float) = 0.5
        _BubbleNoise ("Bubble Noise", 2D) = "white" {}
        _LiquidTex ("Liquid Tex", 2D) = "white" {}
        _WobbleAmount ("Wobble", Float) = 0.1
        _HeatWaveMap ("Heat Wave", 2D) = "white" {}
        _HeatDistortion ("Heat Distortion", Float) = 0.1
        _MeshCenter ("Mesh Center", Vector) = (0, 0, 0, 0)
        _MeshSize ("Mesh Size", Vector) = (1, 1, 1, 0)
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
                float3 worldPos : TEXCOORD1;
            };

            float _BubbleSpeed;
            float4 _MainColor;
            float4 _MeshCenter;
            float4 _MeshSize;
            sampler2D _BubbleNoise;
            sampler2D _LiquidTex;
            float _WobbleAmount;
            sampler2D _HeatWaveMap;
            float _HeatDistortion;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float bubble = tex2D(_BubbleNoise, uv + float2(_Time.y * _BubbleSpeed, 0)).r;
                float heat = tex2D(_HeatWaveMap, uv + float2(_Time.y * 0.1, 0)).r * _HeatDistortion;
                float wobble = sin(uv.x * 20 + _Time.y * 3) * _WobbleAmount;
                uv.y += wobble;

                fixed4 baseCol = tex2D(_LiquidTex, uv);
                baseCol.rgb += bubble * 0.2 + heat * 0.2;
                baseCol *= _MainColor;
                return baseCol;
            }
            ENDCG
        }
    }

    FallBack Off
}
