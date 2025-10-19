Shader "Custom/WireframeFX"
{
    Properties
    {
        _WireColor("Wire Color", Color) = (1,1,1,1)
        _GridScale("Grid Scale", Float) = 0.25
        _LineWidth("Line Width", Range(0.001,0.1)) = 0.02
        _GlowIntensity("Glow Intensity", Range(0,5)) = 0
        _BoilAmount("Boil Amount", Range(0,1)) = 0
        _Humidity("Humidity", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _WireColor;
            float  _GridScale;
            float  _LineWidth;
            float  _GlowIntensity;
            float  _BoilAmount;
            float  _Humidity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wPos : TEXCOORD0;
                float3 nrm  : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.nrm  = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                return o;
            }

            // 線パターンの生成
            float lineMask(float2 uv, float width)
            {
                float2 g = abs(frac(uv) - 0.5);
                float d = min(g.x, g.y);
                float m = step(d, width * 0.5); // 線の内側のみ1
                return m;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 法線によるトライプラナー投影
                float3 n = abs(normalize(i.nrm)) + 1e-5;
                n /= (n.x + n.y + n.z);

                float s = max(_GridScale, 1e-4);
                float mx = lineMask(i.wPos.yz * s, _LineWidth);
                float my = lineMask(i.wPos.zx * s, _LineWidth);
                float mz = lineMask(i.wPos.xy * s, _LineWidth);

                float wire = saturate(mx * n.x + my * n.y + mz * n.z);

                // 面が透明なので、線がない部分は完全に破棄
                if (wire <= 0.001)
                    discard;

                float glow = 1 + _GlowIntensity * (_BoilAmount + _Humidity * 0.2);
                float3 col = _WireColor.rgb * glow;

                return float4(col, 1.0); // 線は不透明に出す
            }
            ENDCG
        }
    }
    FallBack Off
}
