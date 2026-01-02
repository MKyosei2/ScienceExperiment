Shader "ChemLab/ParticleUnlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _SoftFactor ("Soft Factor", Range(0,10)) = 1
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            half _Opacity;
            half _SoftFactor;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 col : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = v.color * _BaseColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = i.col;
                c.a *= _Opacity;
                return c;
            }
            ENDCG
        }
    }

    FallBack Off
}
