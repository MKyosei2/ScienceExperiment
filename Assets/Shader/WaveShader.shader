Shader "ChemLab/WaveShader"
{
    Properties
    {
        _MainColor ("Base Color", Color) = (0.2, 0.5, 1, 1)
        _WaveIntensity ("Wave Intensity", Range(0, 1)) = 0.2
        _Speed ("Wave Speed", Range(0,5)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "3000" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _MainColor;
            float _WaveIntensity;
            float _Speed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;

                // sin-based ripple distortion
                float distort = sin(i.uv.y * 20 + t * 5) * _WaveIntensity;
                distort += sin(i.uv.x * 15 - t * 4) * _WaveIntensity * 0.6;

                float2 uv2 = i.uv + distort;

                return float4(_MainColor.rgb, 0.8);
            }
            ENDCG
        }
    }
}
