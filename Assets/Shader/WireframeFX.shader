// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "VRC Lab/WireframeFX"
{
    Properties
    {
        _GlassColor("Glass Color", Color) = (1,1,1,1)
        _GlassAlpha("Glass Alpha", Range(0,1)) = 0.15

        _WireColor("Wire Color", Color) = (0,1,1,1)
        _WireOpacity("Wire Opacity", Range(0,1)) = 1.0

        _GridScale("Grid Scale", Range(0.1, 50)) = 6.0
        _GridWidth("Grid Width", Range(0.001, 0.2)) = 0.03

        _RimPower("Rim Power", Range(0.5, 10)) = 4.0
        _RimStrength("Rim Strength", Range(0, 3)) = 1.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"   // ★必須：組み込み行列・カメラ位置などを使う

            fixed4 _GlassColor;
            float _GlassAlpha;

            fixed4 _WireColor;
            float _WireOpacity;

            float _GridScale;
            float _GridWidth;

            float _RimPower;
            float _RimStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 posW : TEXCOORD0;
                float3 nrmW : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float3 posW = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.posW = posW;

                // 非一様スケール厳密ではないが提出優先でOK
                o.nrmW = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float GridLine(float3 posW, float scale, float width)
            {
                float3 p = posW * scale;
                float3 f = abs(frac(p) - 0.5);
                float d = min(f.x, min(f.y, f.z));
                return 1.0 - step(width, d);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.nrmW);
                float3 v = normalize(_WorldSpaceCameraPos - i.posW);

                fixed4 glass = _GlassColor;
                glass.a = saturate(_GlassAlpha);

                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimStrength;
                rim = saturate(rim);

                float grid = GridLine(i.posW, _GridScale, _GridWidth);
                float wireMask = saturate(max(grid, rim));

                fixed4 wire = _WireColor;
                wire.a = saturate(_WireOpacity) * wireMask * _WireColor.a;

                fixed4 outCol = glass;
                outCol.rgb = saturate(outCol.rgb + wire.rgb * wire.a);
                outCol.a = saturate(max(glass.a, wire.a));
                return outCol;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
