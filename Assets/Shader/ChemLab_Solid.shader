Shader "ChemLab/Solid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.4
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,5)) = 0
        _NoiseScale ("Noise Scale", Range(0,5)) = 0.25
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _EdgeFresnel ("Edge Fresnel", Range(0,5)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 300

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        fixed4 _BaseColor;
        half _Opacity;
        half _Metallic;
        half _Smoothness;
        fixed4 _EmissionColor;
        half _EmissionStrength;
        half _NoiseScale;
        half _Dissolve;
        half _EdgeFresnel;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        // Cheap hash-based noise (fast, stable)
        inline float hash31(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base
            fixed4 c = _BaseColor;
            c.a *= _Opacity;

            // Dissolve: use world-position noise
            float n = hash31(IN.worldPos * max(0.0001, _NoiseScale));
            // dissolve grows from 0..1, keep a tiny feather by using smoothstep
            float cut = smoothstep(_Dissolve - 0.05, _Dissolve + 0.05, n);
            c.a *= (1.0 - cut);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Occlusion = 1;

            // Fresnel edge boost (subtle)
            float ndv = saturate(dot(normalize(o.Normal), normalize(IN.viewDir)));
            float fres = pow(1.0 - ndv, 3.0) * _EdgeFresnel;

            // Emission
            o.Emission = (_EmissionColor.rgb * _EmissionStrength) + fres;

            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
