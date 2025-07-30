Shader "Custom/GlassMaster"
{
    Properties
    {
        _MainColor("Main Color", Color) = (0.1, 0.3, 0.7, 0.85)
        _LiquidTex("Liquid Texture", 2D) = "white" {}
        _BubbleNoise("Bubble Noise", 2D) = "white" {}
        _WobbleAmount("Wobble Amount", Float) = 0.06
        _PrecipitationColor("Precipitation Color", Color) = (0.8, 0.4, 0.2, 1.0)
        _ReactionProgress("Reaction Progress", Range(0,1)) = 0
        _MeshCenter("Mesh Center", Vector) = (0, 0, 0, 0)
        _MeshSize("Mesh Size", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _LiquidTex;
            sampler2D _BubbleNoise;
            float4 _MainColor;
            float _WobbleAmount;
            float4 _PrecipitationColor;
            float _ReactionProgress;
            float4 _MeshCenter;
            float4 _MeshSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = world;
                o.localPos = (world - _MeshCenter.xyz) / _MeshSize.xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 撹拌による上下揺らぎ
                float wobble = sin(uv.x * 20 + _Time.y * 3) * _WobbleAmount;
                uv.y += wobble;

                // 液体テクスチャ + カラー補間
                fixed4 liquidCol = tex2D(_LiquidTex, uv);
                fixed4 mixedCol = lerp(liquidCol * _MainColor, _MainColor, 0.2);

                // 沈殿：底に近いほど PrecipitationColor を強くブレンド
                float yPos = saturate(1.0 - i.localPos.y); // 下にいくほど1.0
                float precipFactor = smoothstep(0.7, 1.0, yPos * _ReactionProgress);
                mixedCol.rgb = lerp(mixedCol.rgb, _PrecipitationColor.rgb, precipFactor);

                // アルファも MainColor の a を尊重
                mixedCol.a = _MainColor.a;

                return mixedCol;
            }
            ENDCG
        }
    }

    FallBack Off
}
