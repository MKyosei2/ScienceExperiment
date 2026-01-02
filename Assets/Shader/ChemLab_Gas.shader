Shader "ChemLab/GasFog"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.85,0.95,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0.08
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,5)) = 0
        _NoiseScale ("Noise Scale", Range(0,5)) = 0.6
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _FogSoftness ("Fog Softness", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            half _Opacity;
            fixed4 _EmissionColor;
            half _EmissionStrength;
            half _NoiseScale;
            half _Dissolve;
            half _FogSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            inline float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = UnityWorldSpaceViewDir(o.worldPos);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 vp = i.worldPos * max(0.0001, _NoiseScale);
                float n = hash31(vp);

                // dissolve as density cut
                float cut = smoothstep(_Dissolve - 0.05, _Dissolve + 0.05, n);

                float ndv = saturate(dot(normalize(i.normal), normalize(i.viewDir)));
                float fres = pow(1.0 - ndv, 2.0);

                fixed4 c = _BaseColor;
                float a = _Opacity;

                // fog softness: use noise and fresnel
                a *= saturate((n + fres * 0.6) * _FogSoftness);
                a *= (1.0 - cut);

                float3 emis = _EmissionColor.rgb * _EmissionStrength;
                c.rgb += emis;
                c.a = saturate(a);
                return c;
            }
            ENDCG
        }
    }

    FallBack Off
}
