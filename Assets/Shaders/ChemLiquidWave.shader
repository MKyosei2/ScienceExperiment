Shader "Chem/LiquidWave"
{
    Properties
    {
        _Color ("Color", Color) = (0.2,0.8,1,0.8)
        _WaveAmp ("Wave Amp", Float) = 0.05
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _Swirl ("Swirl", Float) = 0.0
        _TimeOffset ("TimeOffset", Float) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _WaveAmp;
            float _WaveSpeed;
            float _Swirl;
            float _TimeOffset;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 nrm : TEXCOORD1; };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                float2 u = f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;

                float t = (_Time.y + _TimeOffset) * _WaveSpeed;
                float n = noise(wp.xz * 2.0 + t);
                float w = (n - 0.5) * 2.0;

                float2 p = wp.xz;
                float ang = atan2(p.y, p.x);
                w += sin(ang * 4.0 + t * 2.0) * _Swirl * 0.6;

                v.vertex.xyz += v.normal * (w * _WaveAmp);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = wp;
                o.nrm = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.nrm);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float diff = saturate(dot(n, l)) * 0.35;

                fixed4 col = _Color;
                col.rgb += diff.xxx;
                return col;
            }
            ENDCG
        }
    }
}
