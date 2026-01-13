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
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        ZTest LEqual

        CGINCLUDE
        #pragma target 2.0
        #include "UnityCG.cginc"

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
            o.posW = mul(unity_ObjectToWorld, v.vertex).xyz;
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

        float WireMask(v2f i)
        {
            float3 n = normalize(i.nrmW);
            float3 v = normalize(_WorldSpaceCameraPos - i.posW);

            float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimStrength;
            rim = saturate(rim);

            float grid = GridLine(i.posW, _GridScale, _GridWidth);

            return saturate(max(grid, rim));
        }

        fixed4 fragGlass(v2f i) : SV_Target
        {
            float m = WireMask(i);

            fixed3 glassRgb = _GlassColor.rgb;
            glassRgb += _WireColor.rgb * (m * 0.12);

            return fixed4(saturate(glassRgb), saturate(_GlassAlpha));
        }

        fixed4 fragWire(v2f i) : SV_Target
        {
            float m = WireMask(i);
            float w = saturate(_WireOpacity) * m * _WireColor.a;

            return fixed4(_WireColor.rgb * w, 0);
        }
        ENDCG

        // ---- Glass (Back -> Front) ----
        Pass
        {
            Name "GlassBack"
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGlass
            ENDCG
        }

        Pass
        {
            Name "GlassFront"
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGlass
            ENDCG
        }

        // ---- Wire (Additive, Back -> Front) ----
        Pass
        {
            Name "WireBack"
            Cull Front
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragWire
            ENDCG
        }

        Pass
        {
            Name "WireFront"
            Cull Back
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragWire
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
