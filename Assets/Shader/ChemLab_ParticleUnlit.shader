Shader "ChemLab/ParticleUnlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _SoftFactor ("Soft Factor", Range(0,10)) = 1

        // World-space AABB clip box (driven by ChemElementSpawner via MaterialPropertyBlock)
        _UseClip ("Use Clip", Float) = 0
        _ClipCenter ("Clip Center (WS)", Vector) = (0,0,0,0)
        _ClipExtents ("Clip Extents (WS)", Vector) = (1,1,1,0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            float _Opacity;
            float _UseClip;
            float4 _ClipCenter;
            float4 _ClipExtents;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 col : COLOR;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // ParticleSystem writes per-particle color into vertex color; keep it.
                o.col = v.color * _BaseColor;

                float4 wpos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wpos.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Clip outside the vessel box (if enabled)
                if (_UseClip > 0.5)
                {
                    float3 d = abs(i.worldPos - _ClipCenter.xyz) - _ClipExtents.xyz;
                    float m = max(d.x, max(d.y, d.z));
                    clip(-m);
                }

                fixed4 c = i.col;
                c.a *= _Opacity;
                return c;
            }
            ENDCG
        }
    }

    FallBack Off
}
