Shader "VRChat/ChemGlass_Universal_Slosh_Wire"
{
    Properties
    {
        // ==== Wire & Zone ====
        [Toggle] _WireEnable ("Enable Wireframe", Float) = 1
        [Toggle] _UseWorldZone("Use World Zone Gate", Float) = 1
        _ZoneCenter ("Zone Center (world)", Vector) = (0,0,0,0)
        _ZoneSize   ("Zone Half-Size (world)", Vector) = (0.5,0.5,0.5,0)

        // ==== Gravity / Axis (完全自動。必要なら手動併用) ====
        [Toggle] _UseGravityUp ("Auto: keep surface world-horizontal", Float) = 1
        _UpAxis      ("(Manual) Up Axis (object)", Vector) = (0,1,0,0) // Autoを切る時のみ使用
        // 高さ正規化の境界（多くのモデルが -0.5..0.5。違う場合だけ微調整）
        _HeightMin   ("Axis Min (obj units)", Float) = -0.5
        _HeightMax   ("Axis Max (obj units)", Float) =  0.5

        // ==== Sloshing（傾き角→振幅 自動化） ====
        [Toggle] _SloshEnable   ("Enable Sloshing", Float) = 1
        _BaseSloshAmp ("Base Amplitude (norm)", Range(0,0.25)) = 0.05 // 基本振幅
        _TiltGain     ("Tilt→Amp Gain", Range(0,4)) = 1.5             // 傾きに対する増幅
        _TiltSoftness ("Tilt Soft-Threshold", Range(0,0.25)) = 0.04    // 小傾き抑制
        _MaxAmpClamp  ("Max Amplitude Clamp", Range(0,0.35)) = 0.12    // 安定化の上限
        _WaveSpeed    ("Wave Speed", Range(0.2,5.0)) = 1.1
        _WaveK        ("Wave Spatial Freq", Range(1,30)) = 10
        _WallFadePow  ("Wall Fade Power", Range(0,6)) = 3.0            // 壁際の減衰（形状非依存）
        _NormalBoost  ("Surface Normal Boost", Range(0,2)) = 0.6

        // ==== Glass base（初期は完全透明） ====
        _GlassTint ("Glass Tint (RGBA=Absorption)", Color) = (0.75,0.9,1,0.0)
        _Opacity   ("Glass Opacity", Range(0,1)) = 0.0
        _IOR       ("Index of Refraction", Range(1.0,1.6)) = 1.33
        _FresnelPow("Fresnel Power", Range(1,8)) = 5
        _RimBoost  ("Rim Boost", Range(0,2)) = 0.5
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 2
        [Toggle] _ZWrite ("ZWrite", Float) = 0

        // ==== Liquid（初期オフ） ====
        _LiquidColor ("Liquid Color (RGBA=Absorption)", Color) = (0.1,0.5,1,0.0)
        _LiquidAlpha ("Liquid Opacity", Range(0,1)) = 0.0
        _FillLevel   ("Fill Level 0-1 (gravity up)", Range(0,1)) = 0.5
        _SurfaceSoft ("Fill Surface Softness", Range(0.0001,0.1)) = 0.01
        _FoamWidth   ("Foam Band Width", Range(0.0,0.1)) = 0.02
        _Turbidity   ("Turbidity (base)", Range(0,1)) = 0.0

        // ==== Gas/Heat（初期オフ） ====
        _HeatAmount  ("Heat Shimmer", Range(0,1)) = 0.0
        _HeatHeight  ("Heated Height (0-1)", Range(0,1)) = 0.25
        _BubbleInt   ("Bubble Intensity", Range(0,1)) = 0.0
        _BubbleSize  ("Bubble Size", Range(0.5,8)) = 2.0
        _BubbleSpeed ("Bubble Rise Speed", Range(0,5)) = 1.2
        _Emission    ("Emission", Range(0,4)) = 0.0
        _SparkleInt  ("Sparkle", Range(0,1)) = 0.0

        // ==== Powder / Precip / Crystal（初期オフ） ====
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

        // ==== Refraction（初期0） ====
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
        LOD 260
        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass { } // _RefractScale=0 なら実コストほぼ無し

        // ===== Pass 1: Base（ゾーン内のみ描画／傾き自動スロッシング） =====
        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // zone
            float _UseWorldZone; float4 _ZoneCenter, _ZoneSize;

            // axis & slosh
            float _UseGravityUp; float4 _UpAxis; float _HeightMin,_HeightMax;
            float _SloshEnable,_BaseSloshAmp,_TiltGain,_TiltSoftness,_MaxAmpClamp,_WaveSpeed,_WaveK,_WallFadePow,_NormalBoost;

            // materials
            fixed4 _GlassTint; float _Opacity,_IOR,_FresnelPow,_RimBoost;
            fixed4 _LiquidColor; float _LiquidAlpha,_FillLevel,_SurfaceSoft,_FoamWidth,_Turbidity;
            float _HeatAmount,_HeatHeight,_BubbleInt,_BubbleSize,_BubbleSpeed,_Emission,_SparkleInt;
            float _RefractScale;
            float _PowderEnable; fixed4 _PowderColor; float _PowderInt,_PowderGrain;
            float _SwirlStrength,_SwirlSpeed,_SettleProg,_DissolveProg;
            float _DepositInt; fixed4 _DepositColor; float _RingInt,_CrystalInt,_CrystalShine;

            sampler2D _GrabTexture;

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
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.nrmW  = UnityObjectToWorldNormal(v.normal);
                o.viewW = _WorldSpaceCameraPos - wp.xyz;
                o.posO  = v.vertex.xyz;
                o.posW  = wp.xyz;
                o.grab  = ComputeGrabScreenPos(o.pos);
                return o;
            }

            // helpers
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
            float insideBox(float3 p, float3 c, float3 h){
                float3 d = abs(p - c) - h;
                return step(max(max(d.x,d.y), d.z), 0.0);
            }

            // UpObj: 重力に対する上方向（オブジェクト空間）
            float3 GetUpObj(){
                if (_UseGravityUp > 0.5)
                    return normalize( -mul((float3x3)unity_WorldToObject, float3(0,-1,0)) );
                else
                    return normalize(_UpAxis.xyz);
            }

            // 容器ローカルの“縦軸”（手動UpAxisを容器軸とみなす）と重力の関係から傾き量/方向を算出
            void TiltInfo(float3 UpObj, out float3 tiltDirObj, out float tiltMag){
                float3 CUp = normalize(_UpAxis.xyz); // 容器軸の想定（モデルのローカルYでOK）
                float3 proj = CUp - UpObj * dot(CUp, UpObj); // 重力水平面への射影
                tiltMag = saturate(length(proj));            // ≈ sin(傾き角)
                tiltDirObj = (tiltMag > 1e-4) ? normalize(proj) : float3(1,0,0);
            }

            // 形状非依存の“壁際減衰”：法線と重力のなす角から推定（壁ほど Up に直交）
            float WallAtten(float3 surfNormalW, float3 UpW, float power){
                float a = abs(dot(normalize(surfNormalW), normalize(UpW))); // 壁で ~0, 天地で ~1
                return pow(a, power);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ---- ゾーンゲート（ゾーン外は完全透明＝Wireのみ） ----
                if (_UseWorldZone > 0.5){
                    if (insideBox(i.posW, _ZoneCenter.xyz, _ZoneSize.xyz) < 0.5) return 0;
                }

                float3 UpObj = GetUpObj();
                float3 TiltDirObj; float tiltMag;
                TiltInfo(UpObj, TiltDirObj, tiltMag);

                float3 Nw = normalize(i.nrmW);
                float3 Vw = normalize(i.viewW);
                float  t  = _Time.y;

                // 高さ正規化（重力方向）
                float h   = dot(i.posO, UpObj);
                float h01 = remap01(h, _HeightMin, _HeightMax);

                // ====== 傾き角→振幅 自動算出（小傾きは抑えて自然に） ======
                float tiltSoft = saturate((tiltMag - _TiltSoftness) / max(1e-4, 1.0 - _TiltSoftness));
                float ampAuto  = (_BaseSloshAmp + _TiltGain * tiltSoft * tiltSoft); // 二乗で自然な立ち上がり
                ampAuto = min(ampAuto, _MaxAmpClamp);

                // ====== スロッシング波 ======
                float fill = _FillLevel;
                float3 UpW   = normalize(mul((float3x3)unity_ObjectToWorld, UpObj));
                float3 dirW  = normalize(mul((float3x3)unity_ObjectToWorld, TiltDirObj));

                if (_SloshEnable > 0.5)
                {
                    // 壁際減衰（法線×重力）：形状に依らず“側面ほど減衰”
                    float edgeAtten = WallAtten(Nw, UpW, _WallFadePow);

                    // 進行波（傾き方向へ走る）
                    float phase = dot(i.posO, TiltDirObj) * _WaveK - t * _WaveSpeed;
                    float wave  = ampAuto * edgeAtten * sin(phase);

                    // 液面の上下
                    fill += wave;

                    // ハイライト用に法線を少し傾ける（見た目近似）
                    float slope = ampAuto * edgeAtten * _WaveK * cos(phase);
                    Nw = normalize(Nw + dirW * (slope * _NormalBoost));
                }

                // 液体マスク & フォーム帯
                float liquidMask = 1.0 - smoothstep(fill - _SurfaceSoft, fill + _SurfaceSoft, h01);
                float foam       = 1.0 - smoothstep(fill - _FoamWidth,   fill + _FoamWidth,   h01);

                // Fresnel
                float fres = pow(1 - saturate(dot(Nw, Vw)), _FresnelPow) * (1 + _RimBoost);

                // 泡は重力に対して上へ
                float3 bubP = i.posO * _BubbleSize + UpObj * (t * _BubbleSpeed);
                float bubbles = smoothstep(0.6,1.0,noise3(bubP)) * _BubbleInt * liquidMask;

                // 底側からの加熱ゆらぎ
                float heatRegion = remap01(h01, 0.0, _HeatHeight);
                float heatMask = (1 - heatRegion) * _HeatAmount;

                // 擬似屈折
                float2 distort = (Nw.xy) * (_IOR - 1) * 0.5;
                distort += (bubbles * 0.6 + heatMask * 0.8) * (hash21(i.grab.xy * 200 + t) - 0.5);
                float2 uv = (i.grab.xy / i.grab.w) + distort * _RefractScale;
                fixed4 refr = tex2D(_GrabTexture, uv);

                // ベース色（ガラス/液体）
                fixed4 glassCol = refr;
                glassCol.rgb = lerp(glassCol.rgb, glassCol.rgb * _GlassTint.rgb, _GlassTint.a);
                glassCol.a   = _Opacity + fres * 0.2;

                fixed4 liqCol = refr;
                liqCol.rgb = lerp(liqCol.rgb, liqCol.rgb * _LiquidColor.rgb, _LiquidColor.a + _Turbidity*0.5);
                liqCol.rgb += (_Emission + _SparkleInt * hash21(i.posO.xz*100 + t*10)) * 0.05;
                liqCol.a   = _LiquidAlpha;

                // 粉体系（必要時のみ）
                float powderAlpha=0, powderTint=0, precipMask=0, ringMask=0, crystalGlow=0;
                if (_PowderEnable > 0.5)
                {
                    float swirlAngle = _SwirlStrength * _SwirlSpeed * t * (1.0 - h01);
                    float3 pSwirl = rotateAxis(i.posO, UpObj, swirlAngle);

                    float n = noise3(pSwirl * _PowderGrain);
                    float thresh = lerp(0.85, 0.4, _PowderInt) + _DissolveProg * 0.3;
                    float grains = smoothstep(thresh, 1.0, n);

                    float settleBand = 1.0 - smoothstep(_SettleProg, _SettleProg + 0.05, h01);
                    grains *= liquidMask * settleBand;

                    powderAlpha = grains * 0.6 * (1.0 - _DissolveProg);
                    powderTint  = grains * 0.7;

                    float bottom = 1.0 - h01;
                    precipMask = pow(saturate(bottom), 3.0) * _DepositInt * liquidMask;
                    precipMask *= smoothstep(0.3, 0.95, noise3(i.posO * (_PowderGrain*0.5)));

                    ringMask = (1.0 - smoothstep(fill - 0.01, fill + 0.01, h01)) * _RingInt; // 液面追従
                    ringMask *= smoothstep(0.3, 0.95, noise3(i.posO * 24.0));

                    float crystalSeed = noise3(i.posO * (_PowderGrain*0.8) + UpObj*(t*0.5));
                    float tw = smoothstep(0.92, 0.98, crystalSeed) * _CrystalInt;
                    crystalGlow = tw * (0.25 + 0.75 * pow(1 - saturate(dot(Nw,Vw)), 4)) * _CrystalShine;
                }

                fixed4 col = lerp(glassCol, liqCol, liquidMask);
                col.rgb = lerp(col.rgb, col.rgb * _PowderColor.rgb, powderTint);
                col.a   = saturate(col.a + powderAlpha * 0.35);
                col.rgb = lerp(col.rgb, col.rgb * _DepositColor.rgb, precipMask);
                col.a   = saturate(col.a + precipMask * 0.15);
                col.rgb = lerp(col.rgb, col.rgb * _DepositColor.rgb, ringMask);
                col.a   = saturate(col.a + ringMask * 0.05);
                col.rgb += foam * 0.25 * _LiquidColor.rgb + crystalGlow;

                return col;
            }
            ENDCG
        }

        // ===== Pass 2: Wireframe（常時） =====
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
                col.a *= (_WireFill< 0.5)? edge : saturate(edge+0.2);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
