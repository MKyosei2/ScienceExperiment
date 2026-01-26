using UdonSharp;
using UnityEngine;

public class HClExplosionReaction : UdonSharpBehaviour
{
    [Header("Scene References (0128Test配下のものを入れる)")]
    public GameObject flaskH;          // CONICAL_FLASK_H
    public GameObject flaskCl;         // CONICAL_FLASK_Cl
    public Transform beakerPourTarget; // BEAKER_EMPTY/PourTarget

    [Header("Optional (自動探索もする)")]
    public Transform spoutH;           // CONICAL_FLASK_H/Spout_H
    public Transform spoutCl;          // CONICAL_FLASK_Cl/Spout_Cl

    [Header("Liquid Visual (子に Liquid を置く)")]
    public Transform liquidFlaskH;     // CONICAL_FLASK_H/Liquid
    public Transform liquidFlaskCl;    // CONICAL_FLASK_Cl/Liquid
    public Transform liquidBeaker;     // BEAKER_EMPTY/Liquid

    [Header("Extra Renderers to Tint (白い部分を染める)")]
    public Renderer[] tintRenderersFlaskH;   // CONICAL_FLASK_Hの白い部分
    public Renderer[] tintRenderersFlaskCl;  // CONICAL_FLASK_Clの白い部分
    public Renderer[] tintRenderersBeaker;   // BEAKER_EMPTYの白い部分

    [Header("Liquid Amount")]
    public float flaskStartFill = 0.75f;
    public float beakerStartFill = 0.0f;
    public float pourRatePerSec = 0.45f;
    public float beakerMaxFill = 0.95f;

    [Header("Pour 判定")]
    public float pourStartAngleDeg = 28f;
    public float pourRadius = 0.18f;
    public float minReactFillEach = 0.08f;

    [Header("Mix Color")]
    public Color colorH = new Color(0.10f, 0.80f, 1.00f, 1.0f);   // H=青
    public Color colorCl = new Color(1.00f, 0.95f, 0.10f, 1.0f);  // Cl=黄
    public Color reactedColor = new Color(1.00f, 0.60f, 0.60f, 1.0f);
    [Range(0f, 1f)] public float liquidAlpha = 0.85f;

    // ============================
    // Fluid Slosh（軽量）
    // ============================
    [Header("Fluid Slosh (軽量流体)")]
    public bool enableFluidSlosh = true;
    [Range(0f, 2f)] public float sloshStrength = 0.9f;
    [Range(0.1f, 20f)] public float sloshDamping = 7.0f;
    [Range(0.1f, 30f)] public float sloshSpring = 12.0f;
    [Range(0f, 40f)] public float maxSurfaceTiltDeg = 18f;

    // ============================
    // 注ぎ口の細い水流 Particle
    // ============================
    [Header("Pour Stream FX")]
    public ParticleSystem pourStreamH;   // Spout_H/StreamParticles
    public ParticleSystem pourStreamCl;  // Spout_Cl/StreamParticles
    public float streamRateMax = 140f;

    // ============================
    // ビーカー渦（かき混ぜ）
    // ============================
    [Header("Beaker Swirl FX")]
    public ParticleSystem beakerSwirlParticles; // BEAKER_EMPTY/SwirlParticles
    public float swirlRotateSpeed = 65f;
    public float swirlBuildSpeed = 2.0f;
    public float swirlDecaySpeed = 1.0f;

    // ============================
    // 波シェーダー制御（任意）
    // ============================
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

    // ============================
    // ✅ 目視デバッグ（動いてるか確認）
    // ============================
    [Header("DEBUG Visual Check (色で状態を確認)")]
    public bool debugVisualize = true;
    public Renderer debugBeakerRenderer;
    public Renderer debugFlaskHRenderer;
    public Renderer debugFlaskClRenderer;

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

    private Quaternion lastRotH, lastRotCl, lastRotB;
    private Vector2 surfTiltH, surfVelH;
    private Vector2 surfTiltCl, surfVelCl;
    private Vector2 surfTiltB, surfVelB;

    private float swirlPower; // 0..1

    private void Start()
    {
        AutoResolve_UdonSafe();

        fillFlaskH = Mathf.Clamp01(flaskStartFill);
        fillFlaskCl = Mathf.Clamp01(flaskStartFill);

        fillBeakerTotal = Mathf.Clamp01(beakerStartFill);
        fillBeakerH = 0f;
        fillBeakerCl = 0f;

        reacted = false;

        CacheRenderersAndTransforms();
        ApplyAllLiquidVisuals(0f, false, false);

        if (explosionLight != null) explosionLight.enabled = false;
        if (explosionParticles != null) explosionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (pourStreamH != null) pourStreamH.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (pourStreamCl != null) pourStreamCl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (beakerSwirlParticles != null) beakerSwirlParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (flaskH != null) lastRotH = flaskH.transform.rotation;
        if (flaskCl != null) lastRotCl = flaskCl.transform.rotation;
        lastRotB = transform.rotation;

        ApplyDebugVisual(false, false);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        bool pouringH = (!reacted && flaskH != null && fillFlaskH > 0.0001f && IsPouringIntoBeaker(flaskH.transform, spoutH));
        bool pouringCl = (!reacted && flaskCl != null && fillFlaskCl > 0.0001f && IsPouringIntoBeaker(flaskCl.transform, spoutCl));

        float pouredH = 0f;
        float pouredCl = 0f;

        if (!reacted && fillBeakerTotal < beakerMaxFill)
        {
            if (pouringH)
            {
                pouredH = Pour(dt, ref fillFlaskH);
                if (pouredH > 0f)
                {
                    fillBeakerH += pouredH;
                    fillBeakerTotal += pouredH;
                }
            }

            if (pouringCl)
            {
                pouredCl = Pour(dt, ref fillFlaskCl);
                if (pouredCl > 0f)
                {
                    fillBeakerCl += pouredCl;
                    fillBeakerTotal += pouredCl;
                }
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
        UpdatePourStream(pourStreamH, pouringH, pouredH);
        UpdatePourStream(pourStreamCl, pouringCl, pouredCl);

        // 渦（注いでる間増える／止まると減衰）
        bool pouringAny = (pouringH || pouringCl);
        float targetSwirl = pouringAny ? 1f : 0f;
        float change = (targetSwirl > swirlPower ? swirlBuildSpeed : swirlDecaySpeed) * dt;
        swirlPower = Mathf.MoveTowards(swirlPower, targetSwirl, change);
        UpdateBeakerSwirl(swirlPower);

        // 見た目更新
        ApplyAllLiquidVisuals(swirlPower, pouringH, pouringCl);

        // ✅ 目視デバッグ
        if (debugVisualize) ApplyDebugVisual(pouringH, pouringCl);
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

    private bool IsPouringIntoBeaker(Transform flaskRoot, Transform spout)
    {
        if (flaskRoot == null || spout == null || beakerPourTarget == null) return false;

        float angle = Vector3.Angle(flaskRoot.up, Vector3.up);
        if (angle < pourStartAngleDeg) return false;

        float dist = Vector3.Distance(spout.position, beakerPourTarget.position);
        if (dist > pourRadius) return false;

        return true;
    }

    private void UpdatePourStream(ParticleSystem ps, bool pouring, float poured)
    {
        if (ps == null) return;

        if (!pouring)
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            return;
        }

        if (!ps.isPlaying) ps.Play(true);

        float dt = Time.deltaTime;
        float rate01 = 0f;
        if (dt > 0f) rate01 = (poured / dt) / Mathf.Max(0.0001f, pourRatePerSec);
        rate01 = Mathf.Clamp01(rate01);

        var emission = ps.emission;
        emission.rateOverTime = Mathf.Lerp(20f, streamRateMax, rate01);
    }

    private void UpdateBeakerSwirl(float power01)
    {
        if (beakerSwirlParticles != null)
        {
            if (power01 > 0.05f)
            {
                if (!beakerSwirlParticles.isPlaying) beakerSwirlParticles.Play(true);
                var emission = beakerSwirlParticles.emission;
                emission.rateOverTime = Mathf.Lerp(0f, 80f, power01);
            }
            else
            {
                if (beakerSwirlParticles.isPlaying)
                    beakerSwirlParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (liquidBeaker != null && power01 > 0.01f)
        {
            float y = swirlRotateSpeed * power01 * Time.deltaTime;
            liquidBeaker.Rotate(0f, y, 0f, Space.Self);
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
        if (beakerSwirlParticles != null)
            beakerSwirlParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (pourStreamH != null) pourStreamH.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (pourStreamCl != null) pourStreamCl.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private float ComputeReactionStrength()
    {
        float amount = Mathf.Clamp01(fillBeakerTotal);

        float sum = fillBeakerH + fillBeakerCl;
        float ratioScore = 0.0f;
        if (sum > 0.0001f)
        {
            float r = fillBeakerH / sum;
            float dist = Mathf.Abs(r - 0.5f) * 2f;
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

            var emission = explosionParticles.emission;
            emission.rateOverTime = 0f;
            short burstCount = (short)Mathf.Clamp((int)(45 * strength), 20, 180);
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, burstCount)
            });
        }
    }

    public void EndFlash()
    {
        if (explosionLight != null) explosionLight.enabled = false;
    }

    // ============================
    // ✅ Visual update (fill/color/wave/slosh + tint)
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

        if (enableFluidSlosh)
        {
            ApplySloshForContainer(flaskH != null ? flaskH.transform : null, liquidFlaskH, fillFlaskH, ref lastRotH, ref surfTiltH, ref surfVelH);
            ApplySloshForContainer(flaskCl != null ? flaskCl.transform : null, liquidFlaskCl, fillFlaskCl, ref lastRotCl, ref surfTiltCl, ref surfVelCl);
            ApplySloshForContainer(transform, liquidBeaker, fillBeakerTotal, ref lastRotB, ref surfTiltB, ref surfVelB);
        }

        // ✅ 白い部分を確実に色付け
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

    private void ApplySloshForContainer(
        Transform container, Transform liquid, float fill01,
        ref Quaternion lastRot, ref Vector2 surfTilt, ref Vector2 surfVel)
    {
        if (container == null || liquid == null) return;
        if (fill01 < 0.02f) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Quaternion cur = container.rotation;
        Quaternion dq = cur * Quaternion.Inverse(lastRot);

        dq.ToAngleAxis(out float angDeg, out Vector3 axis);
        if (angDeg > 180f) angDeg -= 360f;

        Vector3 angVel = Vector3.zero;
        if (Mathf.Abs(angDeg) > 0.0001f)
            angVel = axis.normalized * (angDeg * Mathf.Deg2Rad / dt);

        lastRot = cur;

        Vector2 targetTilt = new Vector2(-angVel.z, angVel.x) * sloshStrength;

        surfVel += (targetTilt - surfTilt) * sloshSpring * dt;
        surfVel -= surfVel * sloshDamping * dt;
        surfTilt += surfVel * dt;

        float maxRad = maxSurfaceTiltDeg * Mathf.Deg2Rad;
        surfTilt.x = Mathf.Clamp(surfTilt.x, -maxRad, maxRad);
        surfTilt.y = Mathf.Clamp(surfTilt.y, -maxRad, maxRad);

        Quaternion tiltRot = Quaternion.Euler(surfTilt.y * Mathf.Rad2Deg, 0f, surfTilt.x * Mathf.Rad2Deg);
        liquid.localRotation = tiltRot;
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

    // ✅ 強い色適用（Unlit/Transparentでも通す）
    private void ForceSetRendererColor(Renderer r, Color c)
    {
        if (r == null) return;

        Material[] mats = r.materials;
        if (mats == null || mats.Length == 0) return;

        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];
            if (m == null) continue;

            m.color = c;

            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_MainColor")) m.SetColor("_MainColor", c);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);

            if (m.HasProperty("_EmissionColor"))
            {
                Color ec = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 1f);
                m.SetColor("_EmissionColor", ec);
            }
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

        if (m.HasProperty("_WaveAmp")) m.SetFloat("_WaveAmp", amp);
        if (m.HasProperty("_WaveSpeed")) m.SetFloat("_WaveSpeed", spd);
        if (m.HasProperty("_Swirl")) m.SetFloat("_Swirl", swirlPower);
        if (m.HasProperty("_TimeOffset")) m.SetFloat("_TimeOffset", Time.time);
    }

    // ============================
    // ✅ 目視デバッグ（どこで止まってるか）
    // ============================
    private void ApplyDebugVisual(bool pouringH, bool pouringCl)
    {
        Color idle = new Color(0.45f, 0.45f, 0.45f, 1f);
        Color pouring = new Color(0.2f, 1.0f, 0.2f, 1f);
        Color ready = new Color(1.0f, 0.6f, 0.1f, 1f);
        Color done = new Color(1.0f, 0.1f, 0.1f, 1f);

        bool readyToReact = (fillBeakerH >= minReactFillEach && fillBeakerCl >= minReactFillEach);

        if (debugBeakerRenderer != null)
        {
            if (reacted) ForceSetRendererColor(debugBeakerRenderer, done);
            else if (readyToReact) ForceSetRendererColor(debugBeakerRenderer, ready);
            else if (pouringH || pouringCl) ForceSetRendererColor(debugBeakerRenderer, pouring);
            else ForceSetRendererColor(debugBeakerRenderer, idle);
        }

        if (debugFlaskHRenderer != null)
        {
            Color c = pouringH ? pouring : idle;
            c *= Mathf.Lerp(0.3f, 1.2f, Mathf.Clamp01(fillFlaskH));
            c.a = 1f;
            ForceSetRendererColor(debugFlaskHRenderer, c);
        }

        if (debugFlaskClRenderer != null)
        {
            Color c = pouringCl ? pouring : idle;
            c *= Mathf.Lerp(0.3f, 1.2f, Mathf.Clamp01(fillFlaskCl));
            c.a = 1f;
            ForceSetRendererColor(debugFlaskClRenderer, c);
        }
    }

    // ============================
    // ✅ Udon安全：Tint対象の自動収集（List禁止）
    // ============================
    private void AutoResolve_UdonSafe()
    {
        if (beakerPourTarget == null) beakerPourTarget = transform;

        if (spoutH == null && flaskH != null) spoutH = flaskH.transform;
        if (spoutCl == null && flaskCl != null) spoutCl = flaskCl.transform;

        if (flaskH != null && (tintRenderersFlaskH == null || tintRenderersFlaskH.Length == 0))
            tintRenderersFlaskH = CollectTintTargetsRuntime_UdonSafe(flaskH.transform);

        if (flaskCl != null && (tintRenderersFlaskCl == null || tintRenderersFlaskCl.Length == 0))
            tintRenderersFlaskCl = CollectTintTargetsRuntime_UdonSafe(flaskCl.transform);

        if (tintRenderersBeaker == null || tintRenderersBeaker.Length == 0)
            tintRenderersBeaker = CollectTintTargetsRuntime_UdonSafe(transform);

        // Debug renderer自動セット（入ってなければ何か拾う）
        if (debugBeakerRenderer == null)
        {
            Renderer rr = GetComponentInChildren<Renderer>(true);
            debugBeakerRenderer = rr;
        }
        if (debugFlaskHRenderer == null && flaskH != null)
        {
            debugFlaskHRenderer = flaskH.GetComponentInChildren<Renderer>(true);
        }
        if (debugFlaskClRenderer == null && flaskCl != null)
        {
            debugFlaskClRenderer = flaskCl.GetComponentInChildren<Renderer>(true);
        }
    }

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
            if (n.Contains("streamparticles")) continue;
            if (n.Contains("swirlparticles")) continue;
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
            if (n.Contains("streamparticles")) continue;
            if (n.Contains("swirlparticles")) continue;
            if (n.Contains("explosion")) continue;

            result[w] = r;
            w++;
            if (w >= count) break;
        }

        return result;
    }
}
