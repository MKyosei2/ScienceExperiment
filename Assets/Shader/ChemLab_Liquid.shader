Shader "ChemLab/Liquid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.7,0.9,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0.12
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.95
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,5)) = 0
        _NoiseScale ("Noise Scale", Range(0,5)) = 0.2
        _Dissolve ("Dissolve", Range(0,1)) = 0

        _Viscosity ("Viscosity", Range(0,2)) = 1
        _Density ("Density", Range(0,2)) = 1
        _Glow ("Glow", Range(0,2)) = 0
        _WaveStrength ("Wave Strength", Range(0,5)) = 0
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

        half _Viscosity;
        half _Density;
        half _Glow;
        half _WaveStrength;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
        };

        inline float hash31(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = _BaseColor;
            c.a *= _Opacity;

            float n = hash31(IN.worldPos * max(0.0001, _NoiseScale));
            float cut = smoothstep(_Dissolve - 0.05, _Dissolve + 0.05, n);
            c.a *= (1.0 - cut);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;

            // pseudo "liquid" modulation: viscosity & density slightly affect emission
            float visc = saturate(_Viscosity / 2.0);
            float dens = saturate(_Density / 2.0);

            float ndv = saturate(dot(normalize(o.Normal), normalize(IN.viewDir)));
            float fres = pow(1.0 - ndv, 4.0);

            float wave = _WaveStrength * 0.2;
            float wobble = (n - 0.5) * wave;

            o.Emission = (_EmissionColor.rgb * _EmissionStrength) + (_Glow * fres) + wobble;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
