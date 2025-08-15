Shader "VRChat/WireframeDX11"
{
    Properties
    {
        _WireColor ("Wire Color", Color) = (0.0, 0.35, 1.0, 1.0)
        _FillColor ("Fill Color", Color) = (0, 0, 0, 0)
        _LineWidth ("Line Width (px)", Range(0.3, 8)) = 1.2
        _Fill      ("Show Fill (0/1)", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0    // Off=0, Front=1, Back=2
        [Toggle] _ZWrite ("ZWrite (show-through off when ON)", Float) = 0   // 0:背面も透けて見える  1:自分で隠す
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 4.0                 // DX11 必須
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _WireColor;
            fixed4 _FillColor;
            float  _LineWidth;
            float  _Fill;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2g {
                float4 pos : SV_POSITION;
            };

            struct g2f {
                float4 pos  : SV_POSITION;
                float3 bary : TEXCOORD0;   // バリセントリック
            };

            v2g vert (appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;

                o.pos = IN[0].pos; o.bary = float3(1,0,0); triStream.Append(o);
                o.pos = IN[1].pos; o.bary = float3(0,1,0); triStream.Append(o);
                o.pos = IN[2].pos; o.bary = float3(0,0,1); triStream.Append(o);
            }

            float min3(float3 v) { return min(v.x, min(v.y, v.z)); }

            fixed4 frag (g2f i) : SV_Target
            {
                // エッジ幅をピクセル基準で一定化（fwidth によるAA）
                float m = min3(i.bary);
                float edge = 1.0 - smoothstep(0.0, fwidth(m) * _LineWidth, m);

                // 線のみ or 塗り＋線
                fixed4 col;
                if (_Fill < 0.5)
                {
                    col = _WireColor;
                    col.a = edge * _WireColor.a;       // 線以外は透過
                }
                else
                {
                    fixed4 fill = _FillColor;
                    col = lerp(fill, _WireColor, edge); // 塗りの上に線
                    // 塗りが完全不透明を避けたいときは _FillColor.a を調整
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
