Shader "VRChat/ChemGlass_ReactionAll_Wire"
{
    Properties
    {
        // ==== Master switches ====
        [Toggle] _BaseVisible ("Base (glass/liquid/powder) Visible", Float) = 0
        [Toggle] _WireEnable  ("Enable Wireframe", Float) = 1
        [Toggle] _UseWorldZone("Use World Zone Gate", Float) = 1
        _StartFX   ("Start FX (0/1)", Range(0,1)) = 0

        // ==== World zone (AABB in world space) ====
        _ZoneCenter ("Zone Center (world)", Vector) = (0,0,0,0)
        _ZoneSize   ("Zone Half-Size (world)", Vector) = (0.5,0.5,0.5,0)

        // ==== Glass base ====
        _GlassTint ("Glass Tint (RGBA=Absorption)", Color) = (0.75,0.9,1,0.0)
        _Opacity   ("Glass Opacity", Range(0,1)) = 0.0
        _IOR       ("Index of Refraction", Range(1.0,1.6)) = 1.33
        _FresnelPow("Fresnel Power", Range(1,8)) = 5
        _RimBoost  ("Rim Boost", Range(0,2)) = 0.5
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 2
        [Toggle] _ZWrite ("ZWrite", Float) = 0

        // ==== Liquid ====
        _LiquidColor ("Liquid Color (RGBA=Absorption)", Color) = (0.1,0.5,1,0.0)
        _LiquidAlpha ("Liquid Opacity", Range(0,1)) = 0.0
        _FillLevel   ("Fill Level 0-1 (along axis)", Range(0,1)) = 0.5
        _FoamWidth   ("Foam Band Width", Range(0.0,0.1)) = 0.02
        _Turbidity   ("Turbidity (base)", Range(0,1)) = 0.0

        // ==== Axis/bounds in object space ====
        _UpAxis      ("Up Axis (object)", Vector) = (0,1,0,0)
        _HeightMin   ("Axis Min (obj)", Float) = -0.5
        _HeightMax   ("Axis Max (obj)", Float) =  0.5
        _SurfaceSoft ("Fill Surface Softness", Range(0.0001,0.1)) = 0.01

        // ==== Gas/heat ====
        _HeatAmount  ("Heat Shimmer", Range(0,1)) = 0.0
        _HeatHeight  ("Heated Height (0-1)", Range(0,1)) = 0.25
        _BubbleInt   ("Bubble Intensity", Range(0,1)) = 0.0
        _BubbleSize  ("Bubble Size", Range(0.5,8)) = 2.0
        _BubbleSpeed ("Bubble Rise Speed", Range(0,5)) = 1.2
        _Emission    ("Emission", Range(0,4)) = 0.0
        _SparkleInt  ("Sparkle", Range(0,1)) = 0.0

        // ==== Powder / Precipitation / Crystal ====
        [Toggle] _PowderEnable ("Enable Powder", Float) = 0
        _PowderColor  ("Powder/Solid Color", Color) = (0.85,0.85,0.85,1)
        _PowderInt    ("Suspension Concentration", Range(0,1)) = 0.0
        _PowderGrain  ("Grain Scale", Range(1,30)) = 12
        _SwirlStrength("Swirl Strength", Range(0,2)) = 0.0
        _SwirlSpeed   ("Swirl Angular Speed", Range(-10,10)) = 3.0
        _SettleProg   ("Settling Progress", Range(0,1)) = 0.0
        _DissolveProg ("Dissolution Progress", Range(0,1)) = 0.0
        _DepositInt   ("Precipitate Amount", Range(0,1)) = 0.0
        _DepositColor ("Precipitate Color", Color) = (0.8,0.7,0.4,1)
        _RingInt      ("Fill-line Ring/Stain", Range(0,1)) = 0.0
        _CrystalInt   ("Crystallization", Range(0,1)) = 0.0
        _CrystalShine ("Crystal Shine", Range(0,2)) = 0.6

        // ==== Refraction ====
        _RefractScale("Screen Distortion", Range(0,0.02)) = 0.0

        // ==== Wireframe ====
        _WireColor  ("Wire Color", Color) = (0.0,0.35,1.0,1)
        _WireWidth  ("Wire Width (px)", Range(0.3,8)) = 1.2
        [Toggle] _WireFill   ("Show Fill Under Wire", Float) = 0
        [Toggle] _WireZWrite ("Wire ZWrite", Float) = 0
    }

    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 250
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass { } // 擬似屈折用（初期オフなので実質透明）

        // ===== Pass 1: Base (ガラス/液体/粉体) with zone gating =====
        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // master & zone
            float _BaseVisible, _StartFX, _UseWorldZone;
            float4 _ZoneCenter, _ZoneSize;

            // materials / params（前回同様）
            fixed4 _GlassTint; float _Opacity,_IOR,_FresnelPow,_RimBoost;
            fixed4 _LiquidColor; float _LiquidAlpha,_FillLevel,_FoamWidth,_Turbidity;
            float4 _UpAxis; float _HeightMin,_HeightMax,_SurfaceSoft;
            float _HeatAmount,_HeatHeight,_BubbleInt,_BubbleSize,_BubbleSpeed,_Emission,_SparkleInt;
            float _RefractScale;
            float _PowderEnable; fixed4 _PowderColor; float _PowderInt,_PowderGrain;
            float _SwirlStrength,_SwirlSpeed,_SettleProg,_DissolveProg;
            float _DepositInt; fixed4 _DepositColor; float _RingInt,_CrystalInt,_CrystalShine;

            sampler2D _GrabTexture; float4 _GrabTexture_TexelSize;

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f {
                float4 pos:SV_POSITION;
                float3 nrmW:TEXCOORD0;
                float3 viewW:TEXCOORD1;
                float3 posO:TEXCOORD2;
                float3 posW:TEXCOORD3;
                float4 grab:TEXCOORD4;
            };

            v2f vert(appdata v){
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.nrmW = UnityObjectToWorldNormal(v.normal);
                o.viewW = _WorldSpaceCameraPos - wp.xyz;
                o.posO = v.vertex.xyz;
                o.posW = wp.xyz;
                o.grab = ComputeGrabScreenPos(o.pos);
                return o;
            }

            float remap01(float x,float a,float b){ return saturate((x-a)/max(b-a,1e-5)); }
            float hash21(float2 p){ p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float3 hash33(float3 p){ p=frac(p*0.3183099+0.1); p*=17.0; return frac(p.xxx*p.yyz*p.zyx); }
            float noise3(float3 p){
                float3 i=floor(p), f=frac(p); float n=0.0;
                [unroll] for(int dx=0;dx<=1;dx++)
                [unroll] for(int dy=0;dy<=1;dy++)
                [unroll] for(int dz=0;dz<=1;dz++){
                    float3 g=float3(dx,dy,dz);
                    float3 r=hash33(i+g)*2-1;
                    float  w=dot(r,f-g)+0.5;
                    float3 s=1-abs(f-g);
                    n+=saturate(w)*s.x*s.y*s.z;
                } return n;
            }
            float3 rotateAxis(float3 p,float3 axis,float angle){
                float c=cos(angle), s=sin(angle); float3 a=normalize(axis);
                return p*c + cross(a,p)*s + a*dot(a,p)*(1-c);
            }
            // AABB in world space
            float insideBox(float3 p, float3 c, float3 h){
                float3 d = abs(p - c) - h;
                return step(max(max(d.x,d.y), d.z), 0.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---- Gate: start switch / visibility / zone ----
                if (_BaseVisible < 0.5 || _StartFX < 0.5) return 0;
                if (_UseWorldZone > 0.5){
                    if (insideBox(i.posW, _ZoneCenter.xyz, _ZoneSize.xyz) < 0.5) return 0;
                }

                float3 N = normalize(i.nrmW);
                float3 V = normalize(i.viewW);
                float3 A = normalize(_UpAxis.xyz);
                float  t = _Time.y;

                float h = dot(i.posO, A);
                float h01 = remap01(h, _HeightMin, _HeightMax);

                float fres = pow(1 - saturate(dot(N,V)), _FresnelPow) * (1 + _RimBoost);

                float surfSoft = _SurfaceSoft;
                float liquidMask = 1.0 - smoothstep(_FillLevel - surfSoft, _FillLevel + surfSoft, h01);
                float foam = 1.0 - smoothstep(_FillLevel - _FoamWidth, _FillLevel + _FoamWidth, h01);

                // Bubbles & heat (multiplied by _StartFX)
                float3 bubP = i.posO * _BubbleSize + A * (t * _BubbleSpeed);
                float bubbles = smoothstep(0.6,1.0,noise3(bubP)) * (_BubbleInt * _StartFX) * liquidMask;

                float heatRegion = remap01(h01, 0.0, _HeatHeight);
                float heatMask = (1 - heatRegion) * (_HeatAmount * _StartFX);

                // Refraction
                float2 distort = (N.xy) * (_IOR - 1) * 0.5;
                distort += (bubbles * 0.6 + heatMask * 0.8) * (hash21(i.grab.xy * 200 + t) - 0.5);
                float2 uv = (i.grab.xy / i.grab.w) + distort * _RefractScale;
                fixed4 refr = tex2D(_GrabTexture, uv);

                fixed4 glassCol = refr;
                glassCol.rgb = lerp(glassCol.rgb, glassCol.rgb * _GlassTint.rgb, _GlassTint.a);
                glassCol.a   = _Opacity + fres * 0.2;

                fixed4 liqCol = refr;
                liqCol.rgb = lerp(liqCol.rgb, liqCol.rgb * _LiquidColor.rgb, _LiquidColor.a + _Turbidity*0.5);
                liqCol.rgb += ((_Emission * _StartFX) + _SparkleInt * hash21(i.posO.xz*100 + t*10)) * 0.05;
                liqCol.a   = _LiquidAlpha;

                // ---- Powder / precip / crystal ----
                float powderAlpha=0, powderTint=0, precipMask=0, ringMask=0, crystalGlow=0;
                if (_PowderEnable > 0.5)
                {
                    float swirlAngle = (_SwirlStrength * _StartFX) * _SwirlSpeed * t * (1.0 - h01);
                    float3 pSwirl = rotateAxis(i.posO, A, swirlAngle);

                    float n = noise3(pSwirl * _PowderGrain);
                    float thresh = lerp(0.85, 0.4, _PowderInt) + _DissolveProg * 0.3;
                    float grains = smoothstep(thresh, 1.0, n);

                    float settleBand = 1.0 - smoothstep(_SettleProg, _SettleProg + 0.05, h01);
                    grains *= liquidMask * settleBand;

                    powderAlpha = grains * 0.6 * (1.0 - _DissolveProg) * _StartFX;
                    powderTint  = grains * 0.7;

                    float bottom = 1.0 - h01;
                    precipMask = pow(saturate(bottom), 3.0) * _DepositInt * liquidMask * _StartFX;
                    precipMask *= smoothstep(0.3, 0.95, noise3(i.posO * (_PowderGrain*0.5)));

                    ringMask = (1.0 - smoothstep(_FillLevel - 0.01, _FillLevel + 0.01, h01)) * _RingInt * _StartFX;
                    ringMask *= smoothstep(0.3, 0.95, noise3(i.posO * 24.0));

                    float crystalSeed = noise3(i.posO * (_PowderGrain*0.8) + A*(t*0.5));
                    float tw = smoothstep(0.92, 0.98, crystalSeed) * _CrystalInt * _StartFX;
                    crystalGlow = tw * (0.25 + 0.75 * pow(1 - saturate(dot(N,V)), 4)) * _CrystalShine;
                }

                fixed4 col = lerp(glassCol, liqCol, liquidMask);
                col.rgb = lerp(col.rgb, col.rgb * _PowderColor.rgb, powderTint);
                col.a   = saturate(col.a + powderAlpha * 0.35);

                col.rgb = lerp(col.rgb, col.rgb * _DepositColor.rgb, precipMask);
                col.a   = saturate(col.a + precipMask * 0.15);

                col.rgb = lerp(col.rgb, col.rgb * _DepositColor.rgb, ringMask);
                col.a   = saturate(col.a + ringMask * 0.05);

                col.rgb += foam * 0.25 * _LiquidColor.rgb;
                col.rgb += crystalGlow;

                col.a = saturate(col.a);
                return col;
            }
            ENDCG
        }

        // ===== Pass 2: Wireframe（常時可視） =====
        Pass
        {
            ZWrite [_WireZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex   v_w
            #pragma geometry g_w
            #pragma fragment f_w
            #include "UnityCG.cginc"

            float  _WireEnable, _WireWidth, _WireFill;
            fixed4 _WireColor;

            struct app_w { float4 vertex:POSITION; };
            struct v2g  { float4 pos:SV_POSITION; };
            struct g2f  { float4 pos:SV_POSITION; float3 bary:TEXCOORD0; };

            v2g v_w(app_w v){ v2g o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            [maxvertexcount(3)]
            void g_w(triangle v2g IN[3], inout TriangleStream<g2f> tri)
            {
                if (_WireEnable < 0.5) return;
                g2f o; o.pos=IN[0].pos; o.bary=float3(1,0,0); tri.Append(o);
                     o.pos=IN[1].pos; o.bary=float3(0,1,0); tri.Append(o);
                     o.pos=IN[2].pos; o.bary=float3(0,0,1); tri.Append(o);
            }
            float min3(float3 v){ return min(v.x, min(v.y, v.z)); }
            fixed4 f_w(g2f i):SV_Target
            {
                if (_WireEnable < 0.5) discard;
                float m=min3(i.bary);
                float edge=1.0 - smoothstep(0.0, fwidth(m) * _WireWidth, m);
                fixed4 col=_WireColor;
                col.a *= (_WireFill<0.5)? edge : saturate(edge+0.2);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
