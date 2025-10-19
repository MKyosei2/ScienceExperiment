Shader "VRC Lab/WireframeFX"
{
    Properties
    {
        _WireColor("Wire Color", Color) = (0,1,1,1)
        _WireThickness("Wire Thickness", Range(0.1, 3.0)) = 1.0
        _Alpha("Surface Alpha", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Wireframe"
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _WireColor;
            float _WireThickness;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 pos : POSITION;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                float3 bary[3] = {
                    float3(1,0,0),
                    float3(0,1,0),
                    float3(0,0,1)
                };

                for (int i = 0; i < 3; i++)
                {
                    o.pos = input[i].pos;
                    o.bary = bary[i];
                    triStream.Append(o);
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // 各ピクセルの三角形内での位置から線を算出
                float3 d = fwidth(i.bary);
                float3 a3 = smoothstep(float3(0.0,0.0,0.0), d * _WireThickness, i.bary);
                float wireMask = min(min(a3.x, a3.y), a3.z);

                // ワイヤー部分だけを描画
                float wireAlpha = 1.0 - wireMask;
                if (wireAlpha <= 0.01)
                    discard;

                fixed4 col = _WireColor;
                col.a = wireAlpha * _WireColor.a * (1.0 - _Alpha);
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
