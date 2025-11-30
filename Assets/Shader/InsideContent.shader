Shader "VRC_ChemLab/InsideContent"
{
    Properties
    {
        _InsideColor ("Inside Color", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0,1)) = 0
        _MaxFill ("Max Fill", Range(0,1)) = 0.8
        _Liquid ("Liquid Mode (1=liquid,0=powder)", Range(0,1)) = 1
        _EmissionPower ("Emission", Range(0,5)) = 0
        _NoiseTex ("Noise Tex", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag            

            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _InsideColor;
            float _FillAmount;
            float _MaxFill;
            float _Liquid;
            float _EmissionPower;

            float3 boundsCenter;
            float3 boundsSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 lp = (i.world - boundsCenter) / boundsSize;
                float fillMask = saturate((_FillAmount * 2.0) - lp.y - 0.5);

                if (fillMask <= 0) return float4(0,0,0,0);

                float noise = tex2D(_NoiseTex, lp.xz * 3).r;
                float density = lerp(noise, 1.0, _Liquid);

                float4 col = _InsideColor;
                col.rgb *= (1 + _EmissionPower * 0.2);
                col.a *= density * fillMask;

                return col;
            }
            ENDCG
        }
    }
}
