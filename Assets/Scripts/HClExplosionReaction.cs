using UdonSharp;
using UnityEngine;

public class HClExplosionReaction : UdonSharpBehaviour
{
    [Header("Scene References")]
    public GameObject flaskH;              // CONICAL_FLASK_H
    public GameObject flaskCl;             // CONICAL_FLASK_Cl
    public Transform beakerPourTarget;     // BEAKER_EMPTY/PourTarget (未設定なら自動検索)

    [Header("Spout References (未設定なら自動検索)")]
    public Transform spoutH;               // CONICAL_FLASK_H/Spout_H
    public Transform spoutCl;              // CONICAL_FLASK_Cl/Spout_Cl

    [Header("Liquid Visual (未設定なら自動検索)")]
    public Transform liquidFlaskH;         // CONICAL_FLASK_H/Liquid
    public Transform liquidFlaskCl;        // CONICAL_FLASK_Cl/Liquid
    public Transform liquidBeaker;         // BEAKER_EMPTY/Liquid

    [Header("Tint Renderers (白い部分を染める)")]
    public Renderer[] tintRenderersFlaskH;
    public Renderer[] tintRenderersFlaskCl;
    public Renderer[] tintRenderersBeaker;

    [Header("Liquid Amount")]
    public float flaskStartFill = 0.75f;
    public float beakerStartFill = 0.0f;
    public float pourRatePerSec = 0.45f;
    public float beakerMaxFill = 0.95f;

    [Header("Pour 判定（基本）")]
    public float pourStartAngleDeg = 28f;
    public float pourRadius = 0.25f;           // XZ半径（近さ）
    public float minReactFillEach = 0.08f;     // H/Cl それぞれがこの量以上で反応

    // =========================
    // ✅ 遠距離誤判定を絶対に潰すための追加条件
    // =========================
    [Header("Pour 判定（誤判定防止）")]
    public float maxPourDistance3D = 0.65f;    // 3D距離の絶対上限
    public float maxVerticalDelta = 0.35f;     // 高さ差が大きすぎたら注げない
    public float minSpoutAboveTarget = 0.03f;  // 注ぎ口がターゲットより上である必要
    [Range(-1f, 1f)] public float minDownDot = 0.20f; // 注ぎ口が下向きの必要度
    public float pourRadiusHardCap = 0.6f;     // 半径の安全上限（事故防止）
    public bool debugPourCheck = false;

    [Header("Mix Color")]
    public Color colorH = new Color(0.10f, 0.80f, 1.00f, 1.0f);   // H=青
    public Color colorCl = new Color(1.00f, 0.95f, 0.10f, 1.0f);  // Cl=黄
    public Color reactedColor = new Color(1.00f, 0.60f, 0.60f, 1.0f);
    [Range(0f, 1f)] public float liquidAlpha = 0.85f;

    // =========================
    // ✅ Fluid Slosh（軽量）
    // =========================
    [Header("Fluid Slosh (軽量流体)")]
    public bool enableFluidSlosh = true;

    [Range(0f, 2f)] public float sloshStrength = 1.15f;
    [Range(0.1f, 30f)] public float sloshSpring = 16.0f;
    [Range(0.1f, 30f)] public float sloshDamping = 6.5f;
    [Range(0f, 45f)] public float maxSurfaceTiltDeg = 28f;

    // VRで「回転しない移動」でも揺れるようにする（効かない対策）
    public bool sloshUseTranslation = true;
    [Range(0f, 2f)] public float translationToTilt = 0.35f;

    // =========================
    // FX
    // =========================
    [Header("Pour Stream FX")]
    public ParticleSystem pourStreamH;
    public ParticleSystem pourStreamCl;
    public float streamRateMax = 140f;

    [Header("Beaker Swirl FX")]
    public ParticleSystem beakerSwirlParticles;
    public float swirlBuildSpeed = 2.0f;
    public float swirlDecaySpeed = 1.0f;

    [Header("Liquid Shader Waves (任意)")]
    public bool enableWaveShader = true;
    public float waveAmpIdle = 0.025f;
    public float waveAmpPouring = 0.075f;
    public float waveSpeed = 1.2f;

    [Header("Reaction FX")]
    public Light explosionLight;
    public float flashSeconds = 0.15f;
    public ParticleSystem explosionParticles;
    public AudioSource explosionAudio;

    [Header("Reaction Strength")]
    [Range(0f, 3f)] public float maxStrengthMultiplier = 2.2f;
    [Range(0f, 1f)] public float ratioBonus = 0.6f;

    [Header("Debug")]
    public bool debugLog = false;

    // ===== State =====
    private bool reacted;

    private float fillFlaskH;
    private float fillFlaskCl;

    private float fillBeakerTotal;
    private float fillBeakerH;
    private float fillBeakerCl;

    private Renderer rFlaskH;
    private Renderer rFlaskCl;
    private Renderer rBeaker;

    private Vector3 baseScaleFlaskH, baseScaleFlaskCl, baseScaleBeaker;
    private Vector3 basePosFlaskH, basePosFlaskCl, basePosBeaker;

    // ✅ base rotation（Sloshを上乗せ）
    private Quaternion baseRotFlaskH = Quaternion.identity;
    private Quaternion baseRotFlaskCl = Quaternion.identity;
    private Quaternion baseRotBeaker = Quaternion.identity;

    // Slosh state
    private Quaternion lastRotH, lastRotCl, lastRotB;
    private Vector3 lastPosH, lastPosCl, lastPosB;
    private Vector2 surfTiltH, surfVelH;
    private Vector2 surfTiltCl, surfVelCl;
    private Vector2 surfTiltB, surfVelB;

    private float swirlPower; // 0..1

    private void Start()
    {
        AutoResolveAll_NoGeneric();

        fillFlaskH = Mathf.Clamp01(flaskStartFill);
        fillFlaskCl = Mathf.Clamp01(flaskStartFill);

        fillBeakerTotal = Mathf.Clamp01(beakerStartFill);
        fillBeakerH = 0f;
        fillBeakerCl = 0f;

        reacted = false;

        CacheRenderersAndTransforms();

        // base rotation保存（Liquid未解決だとSloshが無効化される）
        if (liquidFlaskH != null) baseRotFlaskH = liquidFlaskH.localRotation;
        if (liquidFlaskCl != null) baseRotFlaskCl = liquidFlaskCl.localRotation;
        if (liquidBeaker != null) baseRotBeaker = liquidBeaker.localRotation;

        ApplyAllLiquidVisuals(0f, false, false);

        if (explosionLight != null) explosionLight.enabled = false;
        if (explosionParticles != null) explosionParticles.Stop(true);
        if (pourStreamH != null) pourStreamH.Stop(true);
        if (pourStreamCl != null) pourStreamCl.Stop(true);
        if (beakerSwirlParticles != null) beakerSwirlParticles.Stop(true);

        // Slosh 初期値
        if (flaskH != null)
        {
            lastRotH = flaskH.transform.rotation;
            lastPosH = flaskH.transform.position;
        }
        if (flaskCl != null)
        {
            lastRotCl = flaskCl.transform.rotation;
            lastPosCl = flaskCl.transform.position;
        }
        lastRotB = transform.rotation;
        lastPosB = transform.position;

        if (debugLog)
        {
            Debug.Log("[HClReaction] Start resolved:"
                + " pourTarget=" + (beakerPourTarget != null)
                + " spoutH=" + (spoutH != null)
                + " spoutCl=" + (spoutCl != null)
                + " liquidH=" + (liquidFlaskH != null)
                + " liquidCl=" + (liquidFlaskCl != null)
                + " liquidB=" + (liquidBeaker != null));
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // ✅ 参照が不正なら絶対に注がない（遠距離誤判定の最大原因）
        bool canPourH = (!reacted && flaskH != null && spoutH != null && beakerPourTarget != null && fillFlaskH > 0.0001f);
        bool canPourCl = (!reacted && flaskCl != null && spoutCl != null && beakerPourTarget != null && fillFlaskCl > 0.0001f);

        bool pouringH = canPourH && IsPouringIntoBeaker(flaskH.transform, spoutH);
        bool pouringCl = canPourCl && IsPouringIntoBeaker(flaskCl.transform, spoutCl);

        float pouredH = 0f;
        float pouredCl = 0f;

        if (!reacted && fillBeakerTotal < beakerMaxFill)
        {
            if (pouringH)
            {
                pouredH = Pour(dt, ref fillFlaskH);
                if (pouredH > 0f) { fillBeakerH += pouredH; fillBeakerTotal += pouredH; }
            }

            if (pouringCl)
            {
                pouredCl = Pour(dt, ref fillFlaskCl);
                if (pouredCl > 0f) { fillBeakerCl += pouredCl; fillBeakerTotal += pouredCl; }
            }

            fillBeakerTotal = Mathf.Clamp01(fillBeakerTotal);
            fillBeakerH = Mathf.Clamp01(fillBeakerH);
            fillBeakerCl = Mathf.Clamp01(fillBeakerCl);

            if (fillBeakerH >= minReactFillEach && fillBeakerCl >= minReactFillEach)
            {
                React();
                pouringH = false;
                pouringCl = false;
            }
        }

        // 水流FX
        UpdatePourStream(pourStreamH, pouringH, pouredH, dt);
        UpdatePourStream(pourStreamCl, pouringCl, pouredCl, dt);

        // 渦
        bool pouringAny = (pouringH || pouringCl);
        float targetSwirl = pouringAny ? 1f : 0f;
        float change = (targetSwirl > swirlPower ? swirlBuildSpeed : swirlDecaySpeed) * dt;
        swirlPower = Mathf.MoveTowards(swirlPower, targetSwirl, change);
        UpdateBeakerSwirl(swirlPower);

        // 見た目更新
        ApplyAllLiquidVisuals(swirlPower, pouringH, pouringCl);
    }

    private float Pour(float dt, ref float fillFlask)
    {
        if (fillBeakerTotal >= beakerMaxFill) return 0f;

        float desired = pourRatePerSec * dt;
        float available = Mathf.Max(0f, fillFlask);
        float room = Mathf.Max(0f, beakerMaxFill - fillBeakerTotal);

        float poured = Mathf.Min(desired, Mathf.Min(available, room));
        if (poured <= 0f) return 0f;

        fillFlask -= poured;
        fillFlask = Mathf.Clamp01(fillFlask);
        return poured;
    }

    // =========================================================
    // ✅ 遠距離でも入る判定を完全に潰すロジック（参照ズレも排除）
    // =========================================================
    private bool IsPouringIntoBeaker(Transform flaskRoot, Transform spout)
    {
        if (flaskRoot == null || spout == null || beakerPourTarget == null) return false;

        // spoutがflaskRoot配下じゃない参照ズレは即失敗（IsChildOfは避け、手動で親を辿る）
        if (!IsDescendantOf(spout, flaskRoot)) return false;

        // 角度条件
        float angle = Vector3.Angle(flaskRoot.up, Vector3.up);
        if (angle < pourStartAngleDeg) return false;

        // 半径安全化
        float r = pourRadius;
        if (r > pourRadiusHardCap) r = pourRadiusHardCap;
        if (r < 0.001f) return false;

        Vector3 sp = spout.position;
        Vector3 tp = beakerPourTarget.position;

        // ✅ 3D距離の絶対上限（これで「ものすごく離れてるのに注げる」が理屈上消える）
        float dist3D = Vector3.Distance(sp, tp);
        if (dist3D > maxPourDistance3D) return false;

        // ✅ 高さ差も制限
        float dy = Mathf.Abs(sp.y - tp.y);
        if (dy > maxVerticalDelta) return false;

        // ✅ 注ぎ口がターゲットより上
        if (sp.y < tp.y + minSpoutAboveTarget) return false;

        // ✅ 下向き判定（forward と up の “より下向き” を採用してモデル差を吸収）
        float downDotF = Vector3.Dot(spout.forward, Vector3.down);
        float downDotU = Vector3.Dot(spout.up, Vector3.down);
        float downDot = (downDotF > downDotU) ? downDotF : downDotU;
        if (downDot < minDownDot) return false;

        // ✅ XZ半径（水平距離）
        Vector3 sp2 = sp; sp2.y = 0f;
        Vector3 tp2 = tp; tp2.y = 0f;
        float distXZ = Vector3.Distance(sp2, tp2);
        if (distXZ > r) return false;

        if (debugPourCheck)
        {
            Debug.Log("[PourCheck] angle=" + angle
                + " dist3D=" + dist3D
                + " distXZ=" + distXZ
                + " dy=" + dy
                + " r=" + r
                + " downDot=" + downDot);
        }

        return true;
    }

    private bool IsDescendantOf(Transform child, Transform supposedParent)
    {
        if (child == null || supposedParent == null) return false;

        Transform t = child;
        int guard = 0;
        while (t != null && guard < 128)
        {
            if (t == supposedParent) return true;
            t = t.parent;
            guard++;
        }
        return false;
    }

    private void UpdatePourStream(ParticleSystem ps, bool pouring, float poured, float dt)
    {
        if (ps == null) return;

        if (!pouring)
        {
            if (ps.isPlaying) ps.Stop(true);
            return;
        }

        if (!ps.isPlaying) ps.Play(true);

        float rate01 = 0f;
        if (dt > 0f) rate01 = (poured / dt) / Mathf.Max(0.0001f, pourRatePerSec);
        rate01 = Mathf.Clamp01(rate01);

        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(20f, streamRateMax, rate01);
    }

    private void UpdateBeakerSwirl(float power01)
    {
        if (beakerSwirlParticles == null) return;

        if (power01 > 0.05f)
        {
            if (!beakerSwirlParticles.isPlaying) beakerSwirlParticles.Play(true);
            var emission = beakerSwirlParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 80f, power01);
        }
        else
        {
            if (beakerSwirlParticles.isPlaying)
                beakerSwirlParticles.Stop(true);
        }
    }

    private void React()
    {
        if (reacted) return;
        reacted = true;

        float strength = ComputeReactionStrength();
        if (debugLog) Debug.Log("[HClReaction] REACT strength=" + strength);

        // 反応後：フラスコ空
        fillFlaskH = 0f;
        fillFlaskCl = 0f;

        ApplyBeakerReactedColor();
        ApplyStrengthToFX(strength);

        if (explosionParticles != null) explosionParticles.Play(true);
        if (explosionAudio != null) explosionAudio.Play();

        if (explosionLight != null)
        {
            explosionLight.enabled = true;
            SendCustomEventDelayedSeconds(nameof(EndFlash), flashSeconds);
        }

        swirlPower = 0f;
        if (beakerSwirlParticles != null) beakerSwirlParticles.Stop(true);
        if (pourStreamH != null) pourStreamH.Stop(true);
        if (pourStreamCl != null) pourStreamCl.Stop(true);
    }

    private float ComputeReactionStrength()
    {
        float amount = Mathf.Clamp01(fillBeakerTotal);

        float sum = fillBeakerH + fillBeakerCl;
        float ratioScore = 0.0f;
        if (sum > 0.0001f)
        {
            float rr = fillBeakerH / sum;
            float dist = Mathf.Abs(rr - 0.5f) * 2f;
            ratioScore = 1f - Mathf.Clamp01(dist);
        }

        float strength = amount * (1f + ratioBonus * ratioScore);
        strength = Mathf.Clamp(strength, 0.15f, maxStrengthMultiplier);
        return strength;
    }

    private void ApplyStrengthToFX(float strength)
    {
        if (explosionLight != null)
        {
            explosionLight.intensity = 3.0f * strength;
            explosionLight.range = 2.0f + 1.0f * strength;
        }

        if (explosionAudio != null)
        {
            explosionAudio.volume = Mathf.Clamp01(0.5f + 0.3f * strength);
            explosionAudio.pitch = Mathf.Clamp(0.95f + 0.08f * strength, 0.9f, 1.25f);
        }

        if (explosionParticles != null)
        {
            var main = explosionParticles.main;
            main.startSize = 0.12f * strength;
            main.startSpeed = 1.4f * strength;
            main.maxParticles = Mathf.Clamp((int)(80 * strength), 40, 260);

            // Udonで怪しいBurst設定はやめて、Prefab側でBurst設定する前提
            var emission = explosionParticles.emission;
            emission.rateOverTime = 0f;
        }
    }

    public void EndFlash()
    {
        if (explosionLight != null) explosionLight.enabled = false;
    }

    // ============================
    // Visual update
    // ============================
    private void CacheRenderersAndTransforms()
    {
        if (liquidFlaskH != null)
        {
            rFlaskH = liquidFlaskH.GetComponent<Renderer>();
            baseScaleFlaskH = liquidFlaskH.localScale;
            basePosFlaskH = liquidFlaskH.localPosition;
        }

        if (liquidFlaskCl != null)
        {
            rFlaskCl = liquidFlaskCl.GetComponent<Renderer>();
            baseScaleFlaskCl = liquidFlaskCl.localScale;
            basePosFlaskCl = liquidFlaskCl.localPosition;
        }

        if (liquidBeaker != null)
        {
            rBeaker = liquidBeaker.GetComponent<Renderer>();
            baseScaleBeaker = liquidBeaker.localScale;
            basePosBeaker = liquidBeaker.localPosition;
        }
    }

    private void ApplyAllLiquidVisuals(float swirl01, bool pouringH, bool pouringCl)
    {
        ApplyFillVisual(liquidFlaskH, baseScaleFlaskH, basePosFlaskH, fillFlaskH);
        ApplyFillVisual(liquidFlaskCl, baseScaleFlaskCl, basePosFlaskCl, fillFlaskCl);
        ApplyFillVisual(liquidBeaker, baseScaleBeaker, basePosBeaker, fillBeakerTotal);

        ApplyFlaskColors();
        ApplyBeakerMixedColor();

        if (enableWaveShader)
        {
            float pourBoost = (pouringH || pouringCl) ? 1f : 0f;
            float amp = Mathf.Lerp(waveAmpIdle, waveAmpPouring, pourBoost);
            amp = Mathf.Lerp(amp, amp * 1.5f, swirl01);

            SetWaveParams(rFlaskH, amp * 0.7f, waveSpeed);
            SetWaveParams(rFlaskCl, amp * 0.7f, waveSpeed);
            SetWaveParams(rBeaker, amp, waveSpeed);
        }

        // ✅ Fluid Slosh（回転＋移動で必ず揺れる）
        if (enableFluidSlosh)
        {
            ApplySlosh(flaskH != null ? flaskH.transform : null, liquidFlaskH, fillFlaskH,
                ref lastRotH, ref lastPosH, ref surfTiltH, ref surfVelH, baseRotFlaskH);
            ApplySlosh(flaskCl != null ? flaskCl.transform : null, liquidFlaskCl, fillFlaskCl,
                ref lastRotCl, ref lastPosCl, ref surfTiltCl, ref surfVelCl, baseRotFlaskCl);
            ApplySlosh(transform, liquidBeaker, fillBeakerTotal,
                ref lastRotB, ref lastPosB, ref surfTiltB, ref surfVelB, baseRotBeaker);
        }

        // 白い部分を色付け（任意）
        Color hTint = colorH; hTint.a = 1f;
        Color clTint = colorCl; clTint.a = 1f;
        ApplyTintRenderers(tintRenderersFlaskH, hTint);
        ApplyTintRenderers(tintRenderersFlaskCl, clTint);

        Color beakerTint;
        if (reacted)
        {
            beakerTint = reactedColor;
        }
        else
        {
            float sum = fillBeakerH + fillBeakerCl;
            float t = sum > 0.0001f ? Mathf.Clamp01(fillBeakerCl / sum) : 0f;
            beakerTint = Color.Lerp(colorH, colorCl, t);
        }
        beakerTint.a = 1f;
        ApplyTintRenderers(tintRenderersBeaker, beakerTint);
    }

    private void ApplyFillVisual(Transform liquid, Vector3 baseScale, Vector3 basePos, float fill01)
    {
        if (liquid == null) return;

        float f = Mathf.Clamp01(fill01);

        Vector3 sc = baseScale;
        sc.y = Mathf.Max(0.001f, baseScale.y * f);
        liquid.localScale = sc;

        Vector3 lp = basePos;
        lp.y = basePos.y + Mathf.Abs(baseScale.y) * (f - 1f) * 0.5f;
        liquid.localPosition = lp;

        liquid.gameObject.SetActive(f > 0.01f);
    }

    private void ApplySlosh(
        Transform container, Transform liquid, float fill01,
        ref Quaternion lastRot, ref Vector3 lastPos,
        ref Vector2 surfTilt, ref Vector2 surfVel,
        Quaternion baseRot)
    {
        if (container == null || liquid == null) return;

        // 少量でも動かす（「機能してない」を避ける）
        if (fill01 < 0.003f) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 回転由来
        Quaternion curRot = container.rotation;
        Quaternion dq = curRot * Quaternion.Inverse(lastRot);
        dq.ToAngleAxis(out float angDeg, out Vector3 axis);
        if (angDeg > 180f) angDeg -= 360f;

        Vector3 angVel = Vector3.zero;
        if (Mathf.Abs(angDeg) > 0.0001f)
            angVel = axis.normalized * (angDeg * Mathf.Deg2Rad / dt);

        lastRot = curRot;

        Vector2 targetTilt = new Vector2(-angVel.z, angVel.x) * sloshStrength;

        // 移動由来（VRで持って動かすとこれが効く）
        if (sloshUseTranslation)
        {
            Vector3 curPos = container.position;
            Vector3 vel = (curPos - lastPos) / dt;
            lastPos = curPos;

            targetTilt.x += -vel.x * translationToTilt;
            targetTilt.y += -vel.z * translationToTilt;
        }

        // ばね
        surfVel += (targetTilt - surfTilt) * sloshSpring * dt;
        surfVel -= surfVel * sloshDamping * dt;
        surfTilt += surfVel * dt;

        float maxRad = maxSurfaceTiltDeg * Mathf.Deg2Rad;
        surfTilt.x = Mathf.Clamp(surfTilt.x, -maxRad, maxRad);
        surfTilt.y = Mathf.Clamp(surfTilt.y, -maxRad, maxRad);

        Quaternion tiltRot = Quaternion.Euler(surfTilt.y * Mathf.Rad2Deg, 0f, surfTilt.x * Mathf.Rad2Deg);

        // 初期回転に上乗せ
        liquid.localRotation = baseRot * tiltRot;
    }

    private void ApplyFlaskColors()
    {
        if (rFlaskH != null)
        {
            Color c = colorH; c.a = liquidAlpha;
            ForceSetRendererColor(rFlaskH, c);
        }
        if (rFlaskCl != null)
        {
            Color c = colorCl; c.a = liquidAlpha;
            ForceSetRendererColor(rFlaskCl, c);
        }
    }

    private void ApplyBeakerMixedColor()
    {
        if (rBeaker == null) return;

        if (reacted)
        {
            Color rc = reactedColor; rc.a = liquidAlpha;
            ForceSetRendererColor(rBeaker, rc);
            return;
        }

        float sum = fillBeakerH + fillBeakerCl;
        if (sum <= 0.0001f)
        {
            ForceSetRendererColor(rBeaker, new Color(1f, 1f, 1f, 0f));
            return;
        }

        float t = Mathf.Clamp01(fillBeakerCl / sum);
        Color mix = Color.Lerp(colorH, colorCl, t);
        mix.a = liquidAlpha;
        ForceSetRendererColor(rBeaker, mix);
    }

    private void ApplyBeakerReactedColor()
    {
        if (rBeaker == null) return;
        Color rc = reactedColor; rc.a = liquidAlpha;
        ForceSetRendererColor(rBeaker, rc);
    }

    private void ForceSetRendererColor(Renderer r, Color c)
    {
        if (r == null) return;

        Material[] mats = r.materials;
        if (mats == null || mats.Length == 0) return;

        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];
            if (m == null) continue;

            // HasPropertyを使わず、全部書く（無いプロパティは無視される）
            m.color = c;
            m.SetColor("_Color", c);
            m.SetColor("_BaseColor", c);
            m.SetColor("_MainColor", c);
            m.SetColor("_TintColor", c);

            Color ec = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 1f);
            m.SetColor("_EmissionColor", ec);
        }
    }

    private void ApplyTintRenderers(Renderer[] rs, Color c)
    {
        if (rs == null || rs.Length == 0) return;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            ForceSetRendererColor(r, c);
        }
    }

    private void SetWaveParams(Renderer r, float amp, float spd)
    {
        if (r == null) return;
        Material m = r.material;
        if (m == null) return;

        m.SetFloat("_WaveAmp", amp);
        m.SetFloat("_WaveSpeed", spd);
        m.SetFloat("_Swirl", swirlPower);
        m.SetFloat("_TimeOffset", Time.time);
    }

    // =========================================================
    // ✅ 自動解決（ジェネリック禁止版）
    // =========================================================
    private void AutoResolveAll_NoGeneric()
    {
        // PourTarget
        if (beakerPourTarget == null)
        {
            beakerPourTarget = FindTransformByExactName(transform, "PourTarget");
        }

        // Liquid (Beaker)
        if (liquidBeaker == null)
        {
            liquidBeaker = FindTransformByExactName(transform, "Liquid");
        }

        // Flask H
        if (flaskH != null)
        {
            if (spoutH == null) spoutH = FindTransformByExactName(flaskH.transform, "Spout_H");
            if (spoutH == null) spoutH = FindTransformNameContains(flaskH.transform, "spout");

            if (liquidFlaskH == null) liquidFlaskH = FindTransformByExactName(flaskH.transform, "Liquid");

            if (pourStreamH == null && spoutH != null) pourStreamH = FindParticleSystemByNameContains(spoutH, "stream");
            if (pourStreamH == null) pourStreamH = FindParticleSystemByNameContains(flaskH.transform, "stream");

            if (tintRenderersFlaskH == null || tintRenderersFlaskH.Length == 0)
                tintRenderersFlaskH = CollectTintTargetsRuntime_UdonSafe(flaskH.transform);
        }

        // Flask Cl
        if (flaskCl != null)
        {
            if (spoutCl == null) spoutCl = FindTransformByExactName(flaskCl.transform, "Spout_Cl");
            if (spoutCl == null) spoutCl = FindTransformNameContains(flaskCl.transform, "spout");

            if (liquidFlaskCl == null) liquidFlaskCl = FindTransformByExactName(flaskCl.transform, "Liquid");

            if (pourStreamCl == null && spoutCl != null) pourStreamCl = FindParticleSystemByNameContains(spoutCl, "stream");
            if (pourStreamCl == null) pourStreamCl = FindParticleSystemByNameContains(flaskCl.transform, "stream");

            if (tintRenderersFlaskCl == null || tintRenderersFlaskCl.Length == 0)
                tintRenderersFlaskCl = CollectTintTargetsRuntime_UdonSafe(flaskCl.transform);
        }

        // Optional FX
        if (beakerSwirlParticles == null) beakerSwirlParticles = FindParticleSystemByNameContains(transform, "swirl");
        if (explosionLight == null) explosionLight = FindLightByNameContains(transform, "light");
        if (explosionParticles == null) explosionParticles = FindParticleSystemByNameContains(transform, "explosion");
        if (explosionAudio == null) explosionAudio = FindAudioByNameContains(transform, "audio");

        if (tintRenderersBeaker == null || tintRenderersBeaker.Length == 0)
            tintRenderersBeaker = CollectTintTargetsRuntime_UdonSafe(transform);
    }

    private Transform FindTransformByExactName(Transform root, string exact)
    {
        if (root == null) return null;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && t.name == exact) return t;
        }
        return null;
    }

    private Transform FindTransformNameContains(Transform root, string lowerContains)
    {
        if (root == null) return null;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null) continue;
            string n = t.name;
            if (string.IsNullOrEmpty(n)) continue;
            if (n.ToLower().Contains(lowerContains)) return t;
        }
        return null;
    }

    private ParticleSystem FindParticleSystemByNameContains(Transform root, string lowerContains)
    {
        if (root == null) return null;
        ParticleSystem[] all = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < all.Length; i++)
        {
            ParticleSystem ps = all[i];
            if (ps == null) continue;
            string n = ps.gameObject.name;
            if (string.IsNullOrEmpty(n)) continue;
            if (n.ToLower().Contains(lowerContains)) return ps;
        }
        return null;
    }

    private Light FindLightByNameContains(Transform root, string lowerContains)
    {
        if (root == null) return null;
        Light[] all = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Light l = all[i];
            if (l == null) continue;
            string n = l.gameObject.name;
            if (string.IsNullOrEmpty(n)) continue;
            if (n.ToLower().Contains(lowerContains)) return l;
        }
        return null;
    }

    private AudioSource FindAudioByNameContains(Transform root, string lowerContains)
    {
        if (root == null) return null;
        AudioSource[] all = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < all.Length; i++)
        {
            AudioSource a = all[i];
            if (a == null) continue;
            string n = a.gameObject.name;
            if (string.IsNullOrEmpty(n)) continue;
            if (n.ToLower().Contains(lowerContains)) return a;
        }
        return null;
    }

    // ✅ List禁止（Udon対応）なので2パスで配列生成
    private Renderer[] CollectTintTargetsRuntime_UdonSafe(Transform root)
    {
        if (root == null) return new Renderer[0];

        Renderer[] all = root.GetComponentsInChildren<Renderer>(true);
        if (all == null || all.Length == 0) return new Renderer[0];

        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;

            string n = r.gameObject.name;
            if (string.IsNullOrEmpty(n)) continue;
            n = n.ToLower();

            if (n.Contains("liquid")) continue;
            if (n.Contains("label")) continue;
            if (n.Contains("spout")) continue;
            if (n.Contains("stream")) continue;
            if (n.Contains("swirl")) continue;
            if (n.Contains("explosion")) continue;

            count++;
        }

        if (count <= 0) return new Renderer[0];

        Renderer[] result = new Renderer[count];
        int w = 0;

        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;

            string n = r.gameObject.name;
            if (string.IsNullOrEmpty(n)) continue;
            n = n.ToLower();

            if (n.Contains("liquid")) continue;
            if (n.Contains("label")) continue;
            if (n.Contains("spout")) continue;
            if (n.Contains("stream")) continue;
            if (n.Contains("swirl")) continue;
            if (n.Contains("explosion")) continue;

            result[w] = r;
            w++;
            if (w >= count) break;
        }

        return result;
    }

    // Getter（任意）
    public float GetFillFlaskH() { return fillFlaskH; }
    public float GetFillFlaskCl() { return fillFlaskCl; }
    public float GetFillBeakerTotal() { return fillBeakerTotal; }
    public float GetFillBeakerH() { return fillBeakerH; }
    public float GetFillBeakerCl() { return fillBeakerCl; }
    public bool GetReacted() { return reacted; }
}
