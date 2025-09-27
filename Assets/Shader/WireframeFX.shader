Shader "Custom/WireframeFX"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _WireColor("Wire Color", Color) = (0,1,1,1)
        _GlowIntensity("Glow Intensity", Range(0,5)) = 1

        _BoilAmount("Boil Amount", Range(0,1)) = 0
        _Evaporation("Evaporation", Range(0,1)) = 0
        _ColorShift("Color Shift", Range(-1,1)) = 0
        _Humidity("Humidity", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _WireColor;
            float _GlowIntensity;
            float _BoilAmount;
            float _Evaporation;
            float _ColorShift;
            float _Humidity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 基本色
                float3 col = _BaseColor.rgb;

                // 圧力による色変化
                col = lerp(float3(1,0,0), col, saturate(1.0 - abs(_ColorShift)));
                col = lerp(col, float3(0,0,1), saturate(_ColorShift));

                // 沸騰ノイズ
                float noise = frac(sin(dot(i.worldPos * 10.0, float3(12.9898,78.233,45.164))) * 43758.5453);
                float boil = noise * _BoilAmount;

                // 湿度による曇り（白く濁る）
                col = lerp(col, float3(0.85,0.85,0.85), _Humidity * 0.6);

                // 蒸発: alpha減少
                float alpha = saturate(1.0 - _Evaporation);

                // 発光: 温度依存
                col += _GlowIntensity * 0.2;

                return fixed4(col + boil, alpha);
            }
            ENDCG
        }

        // ワイヤーフレーム用パス
        Pass
        {
            Name "Wireframe"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 _WireColor;

            fixed4 frag (v2f i) : SV_Target
            {
                return _WireColor;
            }
            ENDCG
        }
    }
}
