Shader "ChemLab/ExplosionShader"
{
    Properties
    {
        _Color ("Explosion Color", Color) = (1,0.6,0.2,1)
        _Size ("Glow Size", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+2" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;
            float _Size;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float d = distance(i.uv, float2(0.5, 0.5));
                float glow = saturate((0.5 - d) * 4 * _Size);

                return float4(_Color.rgb * glow, glow);
            }

            ENDCG
        }
    }
}
