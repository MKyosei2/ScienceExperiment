#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text;

[InitializeOnLoad]
public static class ChemLab_VRPlayModeAutoDebugger
{
    private enum Phase
    {
        Idle = 0,
        AcquireRefs = 1,
        Baseline = 2,
        Far_NoPour = 3,
        Near_NotTilt_NoPour = 4,
        Pour_H = 5,
        Pour_Cl = 6,
        Pour_Both_ExpectReaction = 7,
        WrapUp = 8,
        ExitPlayMode = 9,
        Done = 10,
        Failed = 99
    }

    // Session keys
    private const string KEY_RUNNING = "ChemLabVRDbg_Running";
    private const string KEY_PHASE = "ChemLabVRDbg_Phase";
    private const string KEY_PHASE_START = "ChemLabVRDbg_PhaseStart";

    // Params
    private const float STEP_TIMEOUT = 18f; // 少し長め
    private const float MOVE_SPEED = 1.8f;  // m/s
    private const float ROT_SPEED = 240f;   // deg/s
    private const float TILT_DEG = 78f;

    private const float NEAR_Y = 0.12f;
    private const float NEAR_Z = 0.03f;
    private const float FAR_DIST = 1.4f;

    private static StringBuilder _log;

    // Found objects
    private static GameObject root0128;
    private static GameObject beaker;
    private static GameObject flaskH;
    private static GameObject flaskCl;

    private static Transform pourTarget;
    private static Transform spoutH;
    private static Transform spoutCl;

    private static Transform liquidB;
    private static Transform liquidH;
    private static Transform liquidCl;

    private static ParticleSystem streamH;
    private static ParticleSystem streamCl;

    private static Light explosionLight;
    private static ParticleSystem explosionParticles;

    private static Component reaction;

    // Rigidbody override
    private static Rigidbody rbH, rbCl;
    private static bool rbH_prevKinematic, rbCl_prevKinematic;

    // Baseline
    private static Vector3 hPos0, clPos0;
    private static Quaternion hRot0, clRot0;

    private static Vector3 liquidBScale0, liquidHScale0, liquidClScale0;
    private static Color liquidBColor0;

    static ChemLab_VRPlayModeAutoDebugger()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/ChemLab/VR Auto Debugger/Run (PlayMode)")]
    public static void Run()
    {
        EnsureLog();
        LogHeader();
        Log("Run requested.");

        SessionState.SetBool(KEY_RUNNING, true);
        SessionState.SetInt(KEY_PHASE, (int)Phase.AcquireRefs);
        SessionState.SetFloat(KEY_PHASE_START, (float)Time.realtimeSinceStartup);

        ClearRefs();

        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
        else
            Log("Already in PlayMode.");
    }

    [MenuItem("Tools/ChemLab/VR Auto Debugger/Stop (Force)")]
    public static void Stop()
    {
        EnsureLog();
        Log("STOP requested.");
        SessionState.SetBool(KEY_RUNNING, false);
        SessionState.SetInt(KEY_PHASE, (int)Phase.Failed);
        SessionState.SetFloat(KEY_PHASE_START, (float)Time.realtimeSinceStartup);

        RestoreRigidbodies();
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;

        Debug.Log(_log.ToString());
    }

    [MenuItem("Tools/ChemLab/VR Auto Debugger/Print Report")]
    public static void PrintReport()
    {
        if (_log == null) Debug.Log("[ChemLabDebug] No report yet.");
        else Debug.Log(_log.ToString());
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(KEY_RUNNING, false)) return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EnsureLog();
            Log("Entered PlayMode. Starting suite.");
            SessionState.SetFloat(KEY_PHASE_START, (float)Time.realtimeSinceStartup);
            ClearRefs();
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EnsureLog();
            Log("Back to EditMode. Suite ended.");
            SessionState.SetBool(KEY_RUNNING, false);
            RestoreRigidbodies();
            Debug.Log(_log.ToString());
        }
    }

    private static void Update()
    {
        if (!SessionState.GetBool(KEY_RUNNING, false)) return;
        if (!EditorApplication.isPlaying) return;

        EnsureLog();

        try
        {
            Phase phase = (Phase)SessionState.GetInt(KEY_PHASE, (int)Phase.Idle);
            float phaseStart = SessionState.GetFloat(KEY_PHASE_START, 0f);

            if (phase == Phase.Done || phase == Phase.Failed) return;

            float elapsed = Time.realtimeSinceStartup - phaseStart;
            if (elapsed > STEP_TIMEOUT && phase != Phase.ExitPlayMode)
            {
                Fail("Timeout at phase: " + phase);
                return;
            }

            switch (phase)
            {
                case Phase.AcquireRefs:
                    if (AcquireRefs())
                    {
                        SnapshotBaseline();
                        Next(Phase.Baseline);
                    }
                    break;

                case Phase.Baseline:
                    DoBaseline();
                    Next(Phase.Far_NoPour);
                    break;

                case Phase.Far_NoPour:
                    if (TestFarNoPour(elapsed)) Next(Phase.Near_NotTilt_NoPour);
                    break;

                case Phase.Near_NotTilt_NoPour:
                    if (TestNearNotTiltNoPour(elapsed)) Next(Phase.Pour_H);
                    break;

                case Phase.Pour_H:
                    if (TestPourSingle(elapsed, flaskH.transform, spoutH, streamH, liquidH, "H")) Next(Phase.Pour_Cl);
                    break;

                case Phase.Pour_Cl:
                    if (TestPourSingle(elapsed, flaskCl.transform, spoutCl, streamCl, liquidCl, "Cl")) Next(Phase.Pour_Both_ExpectReaction);
                    break;

                case Phase.Pour_Both_ExpectReaction:
                    if (TestPourBothExpectReaction(elapsed)) Next(Phase.WrapUp);
                    break;

                case Phase.WrapUp:
                    PrintHints();
                    Next(Phase.ExitPlayMode);
                    break;

                case Phase.ExitPlayMode:
                    Log("Finished. Exiting PlayMode.");
                    SessionState.SetInt(KEY_PHASE, (int)Phase.Done);
                    RestoreRigidbodies();
                    EditorApplication.isPlaying = false;
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Fail("Exception: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private static void Next(Phase p)
    {
        SessionState.SetInt(KEY_PHASE, (int)p);
        SessionState.SetFloat(KEY_PHASE_START, (float)Time.realtimeSinceStartup);
        Log("---- Next: " + p + " ----");
    }

    private static void Fail(string reason)
    {
        SessionState.SetInt(KEY_PHASE, (int)Phase.Failed);
        SessionState.SetFloat(KEY_PHASE_START, (float)Time.realtimeSinceStartup);

        Log("❌ FAILED: " + reason);
        PrintHints();

        RestoreRigidbodies();
        EditorApplication.isPlaying = false;
    }

    // ========== Acquire ==========
    private static bool AcquireRefs()
    {
        root0128 = GameObject.Find("0128Test");
        if (root0128 == null) return FailAndFalse("Missing root: 0128Test");

        // 深い階層でも拾う（直下前提をやめる）
        beaker = FindDeepGO(root0128.transform, "BEAKER_EMPTY");
        flaskH = FindDeepGO(root0128.transform, "CONICAL_FLASK_H");
        flaskCl = FindDeepGO(root0128.transform, "CONICAL_FLASK_Cl");

        if (beaker == null) return FailAndFalse("Missing: BEAKER_EMPTY under 0128Test");
        if (flaskH == null) return FailAndFalse("Missing: CONICAL_FLASK_H under 0128Test");
        if (flaskCl == null) return FailAndFalse("Missing: CONICAL_FLASK_Cl under 0128Test");

        pourTarget = FindDeep(beaker.transform, "PourTarget");
        spoutH = FindDeep(flaskH.transform, "Spout_H") ?? FindDeepContains(flaskH.transform, "spout");
        spoutCl = FindDeep(flaskCl.transform, "Spout_Cl") ?? FindDeepContains(flaskCl.transform, "spout");

        if (pourTarget == null) return FailAndFalse("Missing: BEAKER_EMPTY/PourTarget");
        if (spoutH == null) return FailAndFalse("Missing spout under CONICAL_FLASK_H (Spout_H or contains 'spout')");
        if (spoutCl == null) return FailAndFalse("Missing spout under CONICAL_FLASK_Cl (Spout_Cl or contains 'spout')");

        liquidB = FindDeep(beaker.transform, "Liquid") ?? FindDeepContains(beaker.transform, "liquid");
        liquidH = FindDeep(flaskH.transform, "Liquid") ?? FindDeepContains(flaskH.transform, "liquid");
        liquidCl = FindDeep(flaskCl.transform, "Liquid") ?? FindDeepContains(flaskCl.transform, "liquid");

        // StreamParticles：spout配下に限定せず、flask全体からも探す
        streamH = FindDeepComponent<ParticleSystem>(spoutH, "StreamParticles")
                  ?? FindDeepComponentContains<ParticleSystem>(flaskH.transform, "stream");
        streamCl = FindDeepComponent<ParticleSystem>(spoutCl, "StreamParticles")
                  ?? FindDeepComponentContains<ParticleSystem>(flaskCl.transform, "stream");

        // 反応検知（任意）
        reaction = beaker.GetComponent("HClExplosionReaction");

        explosionLight = FindDeepComponentContains<Light>(beaker.transform, "explosion")
                         ?? FindDeepComponentContains<Light>(beaker.transform, "light");
        explosionParticles = FindDeepComponentContains<ParticleSystem>(beaker.transform, "explosion")
                             ?? FindDeepComponentContains<ParticleSystem>(beaker.transform, "particle");

        // Rigidbody対策：Editorから動かす間はkinematicにする
        rbH = flaskH.GetComponent<Rigidbody>();
        rbCl = flaskCl.GetComponent<Rigidbody>();
        OverrideRigidbodies();

        Log("Refs acquired.");
        return true;
    }

    private static void OverrideRigidbodies()
    {
        if (rbH != null)
        {
            rbH_prevKinematic = rbH.isKinematic;
            rbH.isKinematic = true;
        }
        if (rbCl != null)
        {
            rbCl_prevKinematic = rbCl.isKinematic;
            rbCl.isKinematic = true;
        }
    }

    private static void RestoreRigidbodies()
    {
        if (rbH != null) rbH.isKinematic = rbH_prevKinematic;
        if (rbCl != null) rbCl.isKinematic = rbCl_prevKinematic;
    }

    private static bool FailAndFalse(string msg)
    {
        Fail(msg);
        return false;
    }

    // ========== Baseline ==========
    private static void SnapshotBaseline()
    {
        hPos0 = flaskH.transform.position;
        clPos0 = flaskCl.transform.position;
        hRot0 = flaskH.transform.rotation;
        clRot0 = flaskCl.transform.rotation;

        liquidBScale0 = (liquidB != null) ? liquidB.localScale : Vector3.zero;
        liquidHScale0 = (liquidH != null) ? liquidH.localScale : Vector3.zero;
        liquidClScale0 = (liquidCl != null) ? liquidCl.localScale : Vector3.zero;

        liquidBColor0 = GetRendererColor(liquidB);

        Log("Baseline snapshot taken.");
    }

    private static void DoBaseline()
    {
        PassFail("HClExplosionReaction exists (optional)", reaction != null,
            "→ BEAKER_EMPTYにHClExplosionReactionが無い/無効化の可能性（ただしテスト自体は続行）");

        PassFail("Liquid objects exist (optional)", (liquidB != null && liquidH != null && liquidCl != null),
            "→ Liquid参照が見つからない：Hierarchyの名前/構造が違う可能性");
    }

    // ========== Tests ==========
    private static bool TestFarNoPour(float elapsed)
    {
        // 遠くで傾ける
        MoveSpoutTo(flaskH.transform, spoutH, pourTarget.position + new Vector3(0f, 0.2f, FAR_DIST));
        Tilt(flaskH.transform, TILT_DEG);

        if (elapsed < 1.4f) return false;

        bool beakerGrew = (liquidB != null) && (liquidB.localScale.y > liquidBScale0.y + 0.003f);
        bool streamOn = (streamH != null && streamH.isPlaying);

        PassFail("Far: beaker should NOT increase", !beakerGrew,
            "→ 遠距離で増える：pourRadius過大 or PourTarget位置が想定より近い");
        PassFail("Far: stream should be OFF (optional)", streamH == null ? true : !streamOn,
            "→ 遠距離でも水流：注ぎ判定が常時trueの可能性");

        RestoreTransforms();
        return true;
    }

    private static bool TestNearNotTiltNoPour(float elapsed)
    {
        // 近いけど傾けない
        MoveSpoutTo(flaskH.transform, spoutH, pourTarget.position + new Vector3(0f, NEAR_Y, NEAR_Z));
        RotateTo(flaskH.transform, hRot0);

        if (elapsed < 1.4f) return false;

        bool beakerGrew = (liquidB != null) && (liquidB.localScale.y > liquidBScale0.y + 0.003f);
        bool streamOn = (streamH != null && streamH.isPlaying);

        PassFail("Near no-tilt: beaker should NOT increase", !beakerGrew,
            "→ 傾けなくても増える：pourStartAngleDegが低すぎ/角度判定が壊れてる");
        PassFail("Near no-tilt: stream should be OFF (optional)", streamH == null ? true : !streamOn,
            "→ 傾けてないのに水流：角度判定が効いてない可能性");

        RestoreTransforms();
        return true;
    }

    private static bool TestPourSingle(float elapsed, Transform flask, Transform spout, ParticleSystem stream, Transform liquid, string label)
    {
        MoveSpoutTo(flask, spout, pourTarget.position + new Vector3(0f, NEAR_Y, NEAR_Z));
        Tilt(flask, TILT_DEG);

        if (elapsed < 3.2f) return false;

        bool streamOn = (stream != null && stream.isPlaying);

        float y0 = (label == "H") ? liquidHScale0.y : liquidClScale0.y;
        bool flaskDecreased = (liquid != null) && (liquid.localScale.y < y0 - 0.005f);
        bool beakerIncreased = (liquidB != null) && (liquidB.localScale.y > liquidBScale0.y + 0.005f);

        PassFail(label + ": stream ON (optional)", stream == null ? true : streamOn,
            "→ 注いでるのに水流なし：Particle参照/再生制御の問題");
        PassFail(label + ": flask liquid decreases (visual)", liquid == null ? true : flaskDecreased,
            "→ フラスコ減らない：Liquid反映/スケール補正/親Scaleの問題");
        PassFail(label + ": beaker increases (visual)", liquidB == null ? true : beakerIncreased,
            "→ ビーカー増えない：注ぎ判定/PourTarget位置/参照の問題");

        RestoreTransforms();
        return true;
    }

    private static bool TestPourBothExpectReaction(float elapsed)
    {
        if (elapsed < 3.2f)
        {
            MoveSpoutTo(flaskH.transform, spoutH, pourTarget.position + new Vector3(0f, NEAR_Y, NEAR_Z));
            Tilt(flaskH.transform, TILT_DEG);
            return false;
        }

        if (elapsed < 6.4f)
        {
            RotateTo(flaskH.transform, hRot0);
            MoveSpoutTo(flaskCl.transform, spoutCl, pourTarget.position + new Vector3(0f, NEAR_Y, NEAR_Z));
            Tilt(flaskCl.transform, TILT_DEG);
            return false;
        }

        bool reactedLight = (explosionLight != null && explosionLight.enabled);
        bool reactedParticle = (explosionParticles != null && explosionParticles.isPlaying);

        Color cNow = GetRendererColor(liquidB);
        bool colorChanged = (liquidB != null) && (ColorDistance(liquidBColor0, cNow) > 0.08f);

        bool flasksEmpty =
            (liquidH == null ? true : (!liquidH.gameObject.activeInHierarchy || liquidH.localScale.y < 0.003f)) &&
            (liquidCl == null ? true : (!liquidCl.gameObject.activeInHierarchy || liquidCl.localScale.y < 0.003f));

        PassFail("Reaction detected (Light OR Particles)", reactedLight || reactedParticle,
            "→ 反応が起きない：反応条件未達 / React呼ばれない / 参照不足");
        PassFail("Beaker color changed (mix/react)", liquidB == null ? true : colorChanged,
            "→ 混色/反応色が見えない：Material色更新が効いてない/Renderer参照漏れ");
        PassFail("After reaction: flasks empty (visual)", flasksEmpty,
            "→ 反応後にフラスコが空にならない：反応後処理 or visual反映不足");

        RestoreTransforms();
        return true;
    }

    // ========== Move / Rotate helpers ==========
    private static float DT()
    {
        // deltaTimeが0でも進むように最低値を入れる
        float dt = Time.deltaTime;
        if (dt < 0.0001f) dt = 1f / 60f;
        return dt;
    }

    private static void MoveSpoutTo(Transform flask, Transform spout, Vector3 desiredSpoutPos)
    {
        // desiredSpoutPos へ spout を合わせるために flask を動かす
        Vector3 delta = desiredSpoutPos - spout.position;

        Vector3 targetFlaskPos = flask.position + delta;

        // VRっぽい微ブレ
        Vector3 jitter = new Vector3(Mathf.Sin(Time.time * 2.3f), 0f, Mathf.Cos(Time.time * 1.9f)) * 0.0005f;
        targetFlaskPos += jitter;

        float dt = DT();
        flask.position = Vector3.MoveTowards(flask.position, targetFlaskPos, MOVE_SPEED * dt);

        // Rigidbodyがあるならpositionで強制（Transform上書き対策）
        Rigidbody rb = flask.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic) rb.position = flask.position;
    }

    private static void Tilt(Transform flask, float tiltDeg)
    {
        Quaternion target = Quaternion.Euler(tiltDeg, flask.eulerAngles.y, flask.eulerAngles.z);
        float dt = DT();
        flask.rotation = Quaternion.RotateTowards(flask.rotation, target, ROT_SPEED * dt);

        Rigidbody rb = flask.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic) rb.rotation = flask.rotation;
    }

    private static void RotateTo(Transform flask, Quaternion target)
    {
        float dt = DT();
        flask.rotation = Quaternion.RotateTowards(flask.rotation, target, ROT_SPEED * dt);

        Rigidbody rb = flask.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic) rb.rotation = flask.rotation;
    }

    private static void RestoreTransforms()
    {
        if (flaskH != null) { flaskH.transform.position = hPos0; flaskH.transform.rotation = hRot0; }
        if (flaskCl != null) { flaskCl.transform.position = clPos0; flaskCl.transform.rotation = clRot0; }

        if (rbH != null && rbH.isKinematic) { rbH.position = hPos0; rbH.rotation = hRot0; }
        if (rbCl != null && rbCl.isKinematic) { rbCl.position = clPos0; rbCl.rotation = clRot0; }
    }

    // ========== Find helpers ==========
    private static GameObject FindDeepGO(Transform root, string exactName)
    {
        Transform t = FindDeep(root, exactName);
        return t != null ? t.gameObject : null;
    }

    private static Transform FindDeep(Transform root, string exactName)
    {
        if (root == null) return null;
        if (root.name == exactName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform r = FindDeep(root.GetChild(i), exactName);
            if (r != null) return r;
        }
        return null;
    }

    private static Transform FindDeepContains(Transform root, string containsLower)
    {
        if (root == null) return null;
        string n = root.name != null ? root.name.ToLower() : "";
        if (n.Contains(containsLower)) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform r = FindDeepContains(root.GetChild(i), containsLower);
            if (r != null) return r;
        }
        return null;
    }

    private static T FindDeepComponent<T>(Transform root, string exactName) where T : Component
    {
        Transform t = FindDeep(root, exactName);
        if (t == null) return null;
        return t.GetComponent<T>();
    }

    private static T FindDeepComponentContains<T>(Transform root, string containsLower) where T : Component
    {
        if (root == null) return null;
        T[] all = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;
            string n = c.gameObject.name != null ? c.gameObject.name.ToLower() : "";
            if (n.Contains(containsLower)) return c;
        }
        return null;
    }

    // ========== Visual probes ==========
    private static Color GetRendererColor(Transform t)
    {
        if (t == null) return Color.black;
        Renderer r = t.GetComponent<Renderer>();
        if (r == null) return Color.black;

        Material m = r.material;
        if (m == null) return Color.black;

        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        return m.color;
    }

    private static float ColorDistance(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        float da = a.a - b.a;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db + da * da);
    }

    // ========== Log ==========
    private static void EnsureLog()
    {
        if (_log == null) _log = new StringBuilder(4096);
    }

    private static void LogHeader()
    {
        EnsureLog();
        _log.AppendLine("====================================================");
        _log.AppendLine(" ChemLab VR PlayMode Auto Debugger Report");
        _log.AppendLine("====================================================");
    }

    private static void Log(string s)
    {
        EnsureLog();
        _log.AppendLine("[ChemLabDebug] " + s);
    }

    private static void PassFail(string title, bool pass, string hint)
    {
        if (pass) Log("✅ PASS: " + title);
        else Log("❌ FAIL: " + title + "  " + hint);
    }

    private static void PrintHints()
    {
        Log("");
        Log("==== Diagnosis Hints ====");
        Log("A) 近づけて傾けても増えない → IsPouring(距離/角度) / spout,pourTarget参照 / pourRadius,pourStartAngleDeg");
        Log("B) ビーカーだけ増える/フラスコ減らない → Liquid視覚反映(Scale/Pos) / 親Scale影響");
        Log("C) 反応しない → minReactFillEach / fillBeakerH,fillBeakerCl加算 / React条件");
        Log("D) Streamが出ない → Particle参照/Play制御/Emission設定");
        Log("========================");
    }

    private static void ClearRefs()
    {
        root0128 = null; beaker = null; flaskH = null; flaskCl = null;
        pourTarget = null; spoutH = null; spoutCl = null;
        liquidB = null; liquidH = null; liquidCl = null;
        streamH = null; streamCl = null;
        explosionLight = null; explosionParticles = null;
        reaction = null;
        rbH = null; rbCl = null;
    }
}
#endif
