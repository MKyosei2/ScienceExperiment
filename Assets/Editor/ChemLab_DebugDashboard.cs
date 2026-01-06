#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChemLab_DebugDashboard : EditorWindow
{
    // ---------------------------
    // Paths (your hierarchy)
    // ---------------------------
    private const string P_Spawner = "Systems/Managers/ChemElementSpawner";
    private const string P_ModeRouter = "Systems/Mode/ModeRouter";
    private const string P_ModeActVR = "Systems/Mode/ModeActivation_VR";
    private const string P_ModeActPC = "Systems/Mode/ModeActivation_PC";
    private const string P_ModeToggle = "UI/Buttons/ModeToggleButton";

    private const string P_VRProps = "World/ExperimentTable/VR_Props";
    private const string P_StartZone = "World/ExperimentTable/Zones/VR_StartZone";
    private const string P_SnapPoints = "World/ExperimentTable/Zones/SnapPoints";

    private const string P_Beaker = "World/ExperimentTable/VR_Props/Beaker_Pickup";
    private const string P_LiquidVolume = "World/ExperimentTable/VR_Props/Beaker_Pickup/LiquidVolume";
    private const string P_StirRod = "World/ExperimentTable/VR_Props/StirRod_Pickup";
    private const string P_TipTrigger = "World/ExperimentTable/VR_Props/StirRod_Pickup/TipTrigger";
    private const string P_Pour = "World/ExperimentTable/VR_Props/PourContainer_Pickup";
    private const string P_Spout = "World/ExperimentTable/VR_Props/PourContainer_Pickup/Spout";

    private const string P_ReactionVFX = "World/ExperimentTable/Effects/ReactionVFX";
    private const string P_LiquidParts = "World/ExperimentTable/VR_Props/Beaker_Pickup/SampleVisual/LiquidParticles";
    private const string P_LiquidSurface = "World/ExperimentTable/VR_Props/Beaker_Pickup/SampleVisual/LiquidSurface";

    // ---------------------------
    // Cached GameObjects
    // ---------------------------
    private GameObject goSpawner, goModeRouter, goModeActVR, goModeActPC, goModeToggle;
    private GameObject goVRProps, goStartZone, goSnapPoints;
    private GameObject goBeaker, goLiquidVol, goStirRod, goTipTrigger, goPour, goSpout;
    private GameObject goReactionVFX, goLiquidParticles, goLiquidSurface;

    // ---------------------------
    // Components (project scripts)
    // ---------------------------
    private ChemElementSpawner spawner;
    private ModeRouter modeRouter;
    private ModeActivation modeActVR;
    private ModeActivation modeActPC;
    private ModeToggleButton modeToggleBtn;

    private VRExperimentInputBridge vrBridge;
    private VRExperimentStartGate vrStartGate;
    private VRStirDetector stirDetector;
    private VRPourDetector pourDetector;
    private VRShakeDetector shakeDetector;

    private ChemReactionAnimator reactionAnimator;
    private ChemSfxAnimator sfxAnimator;

    private LiquidParticleEngine liquidParticleEngine;
    private LiquidPhysicsController liquidPhysicsController;
    private LiquidSurfaceController liquidSurfaceController;

    // ---------------------------
    // Live sampling
    // ---------------------------
    private bool live = true;                 // realtime sampling
    private bool freeze = false;              // stops sampling but keeps UI
    private bool autoRefind = true;           // auto RefreshAll() periodically
    private float refreshHz = 10f;            // sampling rate
    private double nextTick;
    private double nextRefind;
    private const int HistorySize = 240;      // ~24 sec at 10Hz
    private readonly float[] histHeat = new float[HistorySize];
    private readonly float[] histStir = new float[HistorySize];
    private readonly float[] histPour = new float[HistorySize];
    private readonly float[] histShake = new float[HistorySize];
    private int histIndex;
    private bool histFilled;
    private double lastSampleTime;

    // UI state
    private Vector2 scroll;
    private bool foldAuto = true;
    private bool foldMode = true;
    private bool foldVR = true;
    private bool foldVFX = true;
    private bool foldNetwork = true;
    private bool foldActions = true;
    private bool foldGraphs = true;

    [MenuItem("Tools/VRC ChemLab/Debug Dashboard")]
    public static void ShowWindow()
    {
        var w = GetWindow<ChemLab_DebugDashboard>("ChemLab Debug");
        w.minSize = new Vector2(640, 760);
        w.RefreshAll();
        w.Show();
    }

    private void OnEnable()
    {
        RefreshAll();
        EditorApplication.update += EditorTick;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        nextTick = EditorApplication.timeSinceStartup;
        nextRefind = EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorTick;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange _)
    {
        // Clear history on mode changes to avoid stale graphs
        ClearHistory();
        RefreshAll();
        Repaint();
    }

    private void EditorTick()
    {
        if (!live) return;

        double now = EditorApplication.timeSinceStartup;

        // optional: keep references fresh (useful when you reorganize or reimport)
        if (autoRefind && now >= nextRefind)
        {
            nextRefind = now + 1.0; // every 1 sec
            RefreshAll();
        }

        // sampling interval
        float hz = Mathf.Clamp(refreshHz, 1f, 60f);
        double interval = 1.0 / hz;
        if (now < nextTick) return;
        nextTick = now + interval;

        if (freeze) { Repaint(); return; }

        // sample only in play mode, because values are runtime
        if (EditorApplication.isPlaying && spawner != null)
        {
            float h = Safe01(spawner.GetHeat01());
            float s = Safe01(spawner.GetStir01());
            float p = Safe01(spawner.GetPour01());
            float k = Safe01(spawner.GetShake01());

            PushSample(h, s, p, k);
            lastSampleTime = now;
        }

        Repaint();
    }

    private void PushSample(float heat, float stir, float pour, float shake)
    {
        histHeat[histIndex] = heat;
        histStir[histIndex] = stir;
        histPour[histIndex] = pour;
        histShake[histIndex] = shake;

        histIndex++;
        if (histIndex >= HistorySize)
        {
            histIndex = 0;
            histFilled = true;
        }
    }

    private void ClearHistory()
    {
        Array.Clear(histHeat, 0, histHeat.Length);
        Array.Clear(histStir, 0, histStir.Length);
        Array.Clear(histPour, 0, histPour.Length);
        Array.Clear(histShake, 0, histShake.Length);
        histIndex = 0;
        histFilled = false;
    }

    private static float Safe01(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return Mathf.Clamp01(v);
    }

    // =========================================================
    // GUI
    // =========================================================
    private void OnGUI()
    {
        DrawToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawRealtimePanel();
        DrawAutoFoundSection();
        DrawModeSection();
        DrawVRSection();
        DrawGraphsSection();
        DrawVFXSection();
        DrawNetworkSection();
        DrawActionsSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshAll();

            GUILayout.Space(8);
            GUILayout.Label(SceneManager.GetActiveScene().name, EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorApplication.isPlaying ? "PLAY MODE" : "EDIT MODE", EditorStyles.toolbarButton);
        }
    }

    private void DrawRealtimePanel()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Realtime Debug", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                live = EditorGUILayout.ToggleLeft("Live (Editor update loop)", live, GUILayout.Width(220));
                freeze = EditorGUILayout.ToggleLeft("Freeze", freeze, GUILayout.Width(100));
                autoRefind = EditorGUILayout.ToggleLeft("Auto Refresh refs", autoRefind, GUILayout.Width(160));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear History", GUILayout.Width(110)))
                    ClearHistory();
            }

            refreshHz = EditorGUILayout.Slider("Refresh Hz", refreshHz, 1f, 60f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Last Sample Time", GUILayout.Width(120));
                EditorGUILayout.SelectableLabel(
                    (lastSampleTime <= 0) ? "(none)" : lastSampleTime.ToString("0.000"),
                    GUILayout.Height(18)
                );
            }

            if (!EditorApplication.isPlaying)
            {
                DrawInfo("Live values / graphs are available in Play Mode. (Edit Mode = wiring checks only)");
            }
            else if (spawner == null)
            {
                DrawWarn("Spawner not found. Check hierarchy path: Systems/Managers/ChemElementSpawner");
            }
        }
    }

    private void DrawAutoFoundSection()
    {
        foldAuto = EditorGUILayout.Foldout(foldAuto, "1) Auto Found (Hierarchy Paths) / Quick Select", true);
        if (!foldAuto) return;

        EditorGUILayout.Space(4);

        DrawGoRow("Spawner", goSpawner);
        DrawGoRow("ModeRouter", goModeRouter);
        DrawGoRow("ModeActivation_VR", goModeActVR);
        DrawGoRow("ModeActivation_PC", goModeActPC);
        DrawGoRow("ModeToggleButton", goModeToggle);

        GUILayout.Space(6);
        DrawGoRow("VR_Props", goVRProps);
        DrawGoRow("VR_StartZone", goStartZone);
        DrawGoRow("SnapPoints", goSnapPoints);

        GUILayout.Space(6);
        DrawGoRow("Beaker_Pickup", goBeaker);
        DrawGoRow("LiquidVolume", goLiquidVol);
        DrawGoRow("StirRod_Pickup", goStirRod);
        DrawGoRow("TipTrigger", goTipTrigger);
        DrawGoRow("PourContainer_Pickup", goPour);
        DrawGoRow("Spout", goSpout);

        GUILayout.Space(6);
        DrawGoRow("ReactionVFX", goReactionVFX);
        DrawGoRow("LiquidParticles", goLiquidParticles);
        DrawGoRow("LiquidSurface", goLiquidSurface);

        EditorGUILayout.Space(6);
        DrawOverallHealth();
    }

    private void DrawModeSection()
    {
        foldMode = EditorGUILayout.Foldout(foldMode, "2) Mode / Toggle (VR <-> PC)", true);
        if (!foldMode) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (modeRouter == null || modeToggleBtn == null)
            {
                DrawWarn("ModeRouter or ModeToggleButton not found (check hierarchy paths).");
                return;
            }

            EditorGUILayout.LabelField("Current Mode", EditorStyles.boldLabel);
            bool isVR = SafeIsVR(modeRouter);
            bool forcePC = SafeGetForcePC(modeRouter);

            EditorGUILayout.LabelField("IsVR() :", isVR ? "VR" : "PC");
            EditorGUILayout.LabelField("forcePC :", forcePC ? "true (VRでもPC扱い)" : "false (VRはVR扱い)");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = EditorApplication.isPlaying;
                if (GUILayout.Button("Toggle (PlayMode)", GUILayout.Height(26)))
                {
                    try { modeRouter.Toggle(); } catch (Exception e) { Debug.LogException(e); }
                }
                GUI.enabled = true;

                if (GUILayout.Button("Ping ModeRouter")) Ping(goModeRouter);
                if (GUILayout.Button("Ping ModeAct_VR")) Ping(goModeActVR);
                if (GUILayout.Button("Ping ModeAct_PC")) Ping(goModeActPC);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Wiring Checks (must)", EditorStyles.boldLabel);

            DrawBool("ModeToggleButton.modeRouter assigned",
                modeToggleBtn != null && modeToggleBtn.modeRouter != null);

            DrawBool("ModeActivation_VR.router assigned",
                modeActVR != null && modeActVR.router != null);

            DrawBool("ModeActivation_PC.router assigned",
                modeActPC != null && modeActPC.router != null);

            DrawBool("ModeActivation_VR.vrOn contains VR_Props",
                modeActVR != null && ArrayContains(modeActVR.vrOn, goVRProps));

            DrawBool("ModeActivation_VR.notifyOnVR contains VRExperimentInputBridge",
                modeActVR != null && ArrayContains(modeActVR.notifyOnVR, vrBridge));

            DrawBool("ModeActivation_VR.notifyOnVR contains VRExperimentStartGate",
                modeActVR != null && ArrayContains(modeActVR.notifyOnVR, vrStartGate));
        }
    }

    private void DrawVRSection()
    {
        foldVR = EditorGUILayout.Foldout(foldVR, "3) VR Physical Input (Stir/Pour/Shake/StartGate)", true);
        if (!foldVR) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Core References", EditorStyles.boldLabel);
            DrawBool("Spawner found", spawner != null);
            DrawBool("VRExperimentInputBridge found", vrBridge != null);
            DrawBool("VRExperimentStartGate found", vrStartGate != null);
            DrawBool("VRStirDetector found", stirDetector != null);
            DrawBool("VRPourDetector found", pourDetector != null);
            DrawBool("VRShakeDetector found", shakeDetector != null);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Non-script components (must)", EditorStyles.boldLabel);

            DrawColliderStatus("VR_StartZone collider (isTrigger=true)", goStartZone, mustTrigger: true);
            DrawComponentStatus<Rigidbody>("Beaker Rigidbody", goBeaker);
            DrawNonTriggerColliderExists("Beaker has NON-trigger collider", goBeaker);
            DrawColliderStatus("LiquidVolume collider (isTrigger=true)", goLiquidVol, mustTrigger: true);
            DrawColliderStatus("TipTrigger collider (isTrigger=true)", goTipTrigger, mustTrigger: true);
            DrawComponentStatus<Rigidbody>("PourContainer Rigidbody", goPour);
            DrawNonTriggerColliderExists("PourContainer has NON-trigger collider", goPour);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Live Snapshot (Play Mode)", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                DrawInfo("Playモードでリアルタイム値を表示します。");
            }
            else if (spawner == null)
            {
                DrawWarn("Spawner missing => cannot read runtime values.");
            }
            else
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    DrawKV("Heat01", spawner.GetHeat01().ToString("0.000"));
                    DrawKV("Stir01", spawner.GetStir01().ToString("0.000"));
                    DrawKV("Pour01", spawner.GetPour01().ToString("0.000"));
                    DrawKV("Shake01", spawner.GetShake01().ToString("0.000"));

                    DrawKV("HasOperator", spawner.HasOperator() ? "true" : "false");
                    DrawKV("IsOperatorLocal", spawner.IsOperatorLocal() ? "true" : "false");
                    DrawKV("Phase", spawner.GetPhase().ToString());

                    var formula = spawner.GetInputFormula();
                    DrawKV("InputFormula", string.IsNullOrEmpty(formula) ? "(empty)" : formula);

                    var equip = spawner.GetLastEquipment();
                    DrawKV("LastEquipment", string.IsNullOrEmpty(equip) ? "(empty)" : equip);

                    GUILayout.Space(6);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Start Experiment (_StartExperiment)", GUILayout.Height(24)))
                        {
                            try { spawner._StartExperiment(); }
                            catch (Exception e) { Debug.LogException(e); }
                        }

                        if (GUILayout.Button("RequestSerialization (if exists)", GUILayout.Height(24)))
                        {
                            TryCall(spawner, "RequestSerialization");
                        }
                    }
                }
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("VRPourDetector wiring (must)", EditorStyles.boldLabel);
            if (pourDetector != null)
            {
                DrawBool("spout assigned", pourDetector.spout != null);
                DrawBool("target assigned", pourDetector.target != null);
            }
            else
            {
                DrawWarn("VRPourDetector not found");
            }
        }
    }

    private void DrawGraphsSection()
    {
        foldGraphs = EditorGUILayout.Foldout(foldGraphs, "4) Realtime Graphs (Heat/Stir/Pour/Shake)", true);
        if (!foldGraphs) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (!EditorApplication.isPlaying)
            {
                DrawInfo("Graphs are populated in Play Mode (sampling).");
                return;
            }
            if (spawner == null)
            {
                DrawWarn("Spawner missing => graph sampling stops.");
                return;
            }

            DrawGraph("Heat01 (0..1)", histHeat);
            DrawGraph("Stir01 (0..1)", histStir);
            DrawGraph("Pour01 (0..1)", histPour);
            DrawGraph("Shake01 (0..1)", histShake);

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Snapshot to Clipboard", GUILayout.Height(22)))
                {
                    string text =
                        $"Heat01={spawner.GetHeat01():0.000}\n" +
                        $"Stir01={spawner.GetStir01():0.000}\n" +
                        $"Pour01={spawner.GetPour01():0.000}\n" +
                        $"Shake01={spawner.GetShake01():0.000}\n" +
                        $"HasOperator={spawner.HasOperator()}\n" +
                        $"IsOperatorLocal={spawner.IsOperatorLocal()}\n" +
                        $"Phase={spawner.GetPhase()}\n" +
                        $"InputFormula={spawner.GetInputFormula()}\n" +
                        $"LastEquipment={spawner.GetLastEquipment()}";
                    EditorGUIUtility.systemCopyBuffer = text;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(histFilled ? $"History: {HistorySize} samples" : $"History: {histIndex} samples");
            }
        }
    }

    private void DrawVFXSection()
    {
        foldVFX = EditorGUILayout.Foldout(foldVFX, "5) VFX / Particles (existence + wiring)", true);
        if (!foldVFX) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("ReactionVFX", EditorStyles.boldLabel);

            DrawBool("ReactionVFX found", goReactionVFX != null);
            DrawBool("ChemReactionAnimator found", reactionAnimator != null);
            DrawBool("ChemSfxAnimator found", sfxAnimator != null);

            if (goReactionVFX != null)
            {
                var particlesRoot = goReactionVFX.transform.Find("Particles");
                var audioRoot = goReactionVFX.transform.Find("Audio");

                DrawBool("Particles root exists (ReactionVFX/Particles)", particlesRoot != null);
                DrawBool("Audio root exists (ReactionVFX/Audio)", audioRoot != null);

                string[] names = { "Foam", "Smoke", "Spark", "Glint", "Precipitate", "Bubble", "Fog" };
                foreach (var n in names)
                {
                    var t = particlesRoot != null ? particlesRoot.Find(n) : null;
                    bool ok = t != null && t.GetComponent<ParticleSystem>() != null;
                    DrawBool($"ParticleSystem exists: {n}", ok);
                }
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("LiquidParticles (Beaker)", EditorStyles.boldLabel);
            DrawBool("LiquidParticles object found", goLiquidParticles != null);
            if (goLiquidParticles != null)
            {
                DrawBool("ParticleSystem exists", goLiquidParticles.GetComponent<ParticleSystem>() != null);
                DrawBool("LiquidParticleEngine found", liquidParticleEngine != null);
                DrawBool("LiquidPhysicsController found", liquidPhysicsController != null);

                if (liquidParticleEngine != null)
                    DrawBool("LiquidParticleEngine.particle assigned", GetFieldObj(liquidParticleEngine, "particle") != null);

                if (liquidPhysicsController != null)
                    DrawBool("LiquidPhysicsController.particle assigned", GetFieldObj(liquidPhysicsController, "particle") != null);
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("LiquidSurface (optional gas)", EditorStyles.boldLabel);
            DrawBool("LiquidSurface object found", goLiquidSurface != null);
            DrawBool("LiquidSurfaceController found", liquidSurfaceController != null);

            if (goLiquidSurface != null)
            {
                var gas = goLiquidSurface.transform.Find("GasParticle");
                DrawBool("GasParticle child exists", gas != null);
                DrawBool("GasParticle has ParticleSystem", gas != null && gas.GetComponent<ParticleSystem>() != null);
            }
        }
    }

    private void DrawNetworkSection()
    {
        foldNetwork = EditorGUILayout.Foldout(foldNetwork, "6) Network / VRChat Components (SDK present => check)", true);
        if (!foldNetwork) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            var tPickup = FindTypeInAppDomain("VRC.SDK3.Components.VRCPickup");
            var tSync = FindTypeInAppDomain("VRC.SDK3.Components.VRCObjectSync");
            var tInter = FindTypeInAppDomain("VRC.SDK3.Components.VRCInteractable");

            DrawBool("VRChat SDK3 detected (VRCPickup)", tPickup != null);
            DrawBool("VRChat SDK3 detected (VRCObjectSync)", tSync != null);
            DrawBool("VRChat SDK3 detected (VRCInteractable)", tInter != null);

            GUILayout.Space(6);

            DrawVrcCompRow("Beaker", goBeaker, tPickup, tSync, tInter);
            DrawVrcCompRow("StirRod", goStirRod, tPickup, tSync, tInter);
            DrawVrcCompRow("PourContainer", goPour, tPickup, tSync, tInter);

            GUILayout.Space(6);
            DrawInfo("※ SDKが無い場合は false でも正常（Unity側で型が見えないため）。");
        }
    }

    private void DrawActionsSection()
    {
        foldActions = EditorGUILayout.Foldout(foldActions, "7) Actions (validate / run setup / save)", true);
        if (!foldActions) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Quick Checks", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Wiring (log to Console)", GUILayout.Height(24)))
                ValidateToConsole();

            if (GUILayout.Button("Select key objects", GUILayout.Height(24)))
            {
                var list = new List<UnityEngine.Object>();
                AddIf(list, goSpawner);
                AddIf(list, goModeRouter);
                AddIf(list, goModeActVR);
                AddIf(list, goModeActPC);
                AddIf(list, goModeToggle);
                AddIf(list, goVRProps);
                AddIf(list, goStartZone);
                AddIf(list, goReactionVFX);
                Selection.objects = list.ToArray();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("One-Time Setup Runner", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run: One-Time Add Non-Script Components", GUILayout.Height(26)))
                {
                    if (!TryInvokeStaticMenu("ChemLab_OneTime_AddNonScriptComponents", "Run"))
                    {
                        EditorUtility.DisplayDialog(
                            "Not Found",
                            "ChemLab_OneTime_AddNonScriptComponents.Run() が見つかりません。\nAssets/Editor にそのスクリプトがあるか確認してください。",
                            "OK"
                        );
                    }
                    RefreshAll();
                }

                if (GUILayout.Button("Save Scene", GUILayout.Height(26)))
                    EditorSceneManager.SaveOpenScenes();
            }
        }
    }

    // =========================================================
    // Refresh / Validate
    // =========================================================
    private void RefreshAll()
    {
        goSpawner = FindGO(P_Spawner);
        goModeRouter = FindGO(P_ModeRouter);
        goModeActVR = FindGO(P_ModeActVR);
        goModeActPC = FindGO(P_ModeActPC);
        goModeToggle = FindGO(P_ModeToggle);

        goVRProps = FindGO(P_VRProps);
        goStartZone = FindGO(P_StartZone);
        goSnapPoints = FindGO(P_SnapPoints);

        goBeaker = FindGO(P_Beaker);
        goLiquidVol = FindGO(P_LiquidVolume);
        goStirRod = FindGO(P_StirRod);
        goTipTrigger = FindGO(P_TipTrigger);
        goPour = FindGO(P_Pour);
        goSpout = FindGO(P_Spout);

        goReactionVFX = FindGO(P_ReactionVFX);
        goLiquidParticles = FindGO(P_LiquidParts);
        goLiquidSurface = FindGO(P_LiquidSurface);

        spawner = goSpawner ? goSpawner.GetComponent<ChemElementSpawner>() : null;
        modeRouter = goModeRouter ? goModeRouter.GetComponent<ModeRouter>() : null;
        modeActVR = goModeActVR ? goModeActVR.GetComponent<ModeActivation>() : null;
        modeActPC = goModeActPC ? goModeActPC.GetComponent<ModeActivation>() : null;
        modeToggleBtn = goModeToggle ? goModeToggle.GetComponent<ModeToggleButton>() : null;

        vrBridge = FindComponentInScene<VRExperimentInputBridge>();
        vrStartGate = goStartZone ? goStartZone.GetComponent<VRExperimentStartGate>() : null;

        stirDetector = goTipTrigger ? goTipTrigger.GetComponent<VRStirDetector>() : null;
        pourDetector = goPour ? goPour.GetComponent<VRPourDetector>() : null;
        shakeDetector = goBeaker ? goBeaker.GetComponent<VRShakeDetector>() : null;

        reactionAnimator = goReactionVFX ? goReactionVFX.GetComponent<ChemReactionAnimator>() : null;
        sfxAnimator = goReactionVFX ? goReactionVFX.GetComponent<ChemSfxAnimator>() : null;

        liquidParticleEngine = goLiquidParticles ? goLiquidParticles.GetComponent<LiquidParticleEngine>() : null;
        liquidPhysicsController = goLiquidParticles ? goLiquidParticles.GetComponent<LiquidPhysicsController>() : null;
        liquidSurfaceController = goLiquidSurface ? goLiquidSurface.GetComponent<LiquidSurfaceController>() : null;
    }

    private void ValidateToConsole()
    {
        Debug.Log("=== [ChemLab Debug] Validate Wiring ===");

        LogReq("Spawner", spawner != null);
        LogReq("ModeRouter", modeRouter != null);
        LogReq("ModeToggleButton", modeToggleBtn != null);
        LogReq("ModeToggleButton.modeRouter", modeToggleBtn != null && modeToggleBtn.modeRouter != null);

        LogReq("ModeActivation_VR", modeActVR != null);
        LogReq("ModeActivation_VR.router", modeActVR != null && modeActVR.router != null);
        LogReq("ModeActivation_VR.vrOn contains VR_Props", modeActVR != null && ArrayContains(modeActVR.vrOn, goVRProps));
        LogReq("ModeActivation_VR.notifyOnVR contains VRBridge", modeActVR != null && ArrayContains(modeActVR.notifyOnVR, vrBridge));
        LogReq("ModeActivation_VR.notifyOnVR contains StartGate", modeActVR != null && ArrayContains(modeActVR.notifyOnVR, vrStartGate));

        LogReq("VR_StartZone trigger collider", HasCollider(goStartZone, mustTrigger: true));
        LogReq("Beaker Rigidbody", HasComp<Rigidbody>(goBeaker));
        LogReq("Beaker NON-trigger collider", HasNonTriggerCollider(goBeaker));
        LogReq("LiquidVolume trigger collider", HasCollider(goLiquidVol, mustTrigger: true));
        LogReq("TipTrigger trigger collider", HasCollider(goTipTrigger, mustTrigger: true));

        LogReq("ReactionVFX exists", goReactionVFX != null);
        LogReq("ChemReactionAnimator exists", reactionAnimator != null);
        LogReq("ChemSfxAnimator exists", sfxAnimator != null);

        Debug.Log("=== [ChemLab Debug] End ===");
    }

    // =========================================================
    // Graph drawing
    // =========================================================
    private void DrawGraph(string label, float[] hist)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        Rect r = GUILayoutUtility.GetRect(10, 64, GUILayout.ExpandWidth(true));
        DrawGraphRect(r, hist);
    }

    private void DrawGraphRect(Rect r, float[] hist)
    {
        // background
        EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.12f));

        // border
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), new Color(0f, 0f, 0f, 0.25f));
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), new Color(0f, 0f, 0f, 0.25f));
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), new Color(0f, 0f, 0f, 0.25f));
        EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), new Color(0f, 0f, 0f, 0.25f));

        // mid line
        EditorGUI.DrawRect(new Rect(r.x, r.y + r.height * 0.5f, r.width, 1), new Color(0f, 0f, 0f, 0.20f));

        int count = histFilled ? HistorySize : histIndex;
        if (count <= 1)
        {
            GUI.Label(r, "No samples yet", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        // plot from oldest -> newest
        int start = histFilled ? histIndex : 0;

        Vector2 prev = Vector2.zero;
        bool hasPrev = false;

        for (int i = 0; i < count; i++)
        {
            int idx = (start + i) % HistorySize;
            float v = Mathf.Clamp01(hist[idx]);

            float x = r.x + (r.width - 2) * (i / (float)(count - 1)) + 1;
            float y = r.yMax - 1 - (r.height - 2) * v;

            var p = new Vector2(x, y);

            if (hasPrev)
                Handles.DrawLine(prev, p);

            prev = p;
            hasPrev = true;
        }
    }

    // =========================================================
    // GUI helpers
    // =========================================================
    private void DrawOverallHealth()
    {
        int missing = 0;
        missing += spawner == null ? 1 : 0;
        missing += modeRouter == null ? 1 : 0;
        missing += modeToggleBtn == null ? 1 : 0;
        missing += (goVRProps == null) ? 1 : 0;
        missing += (goStartZone == null) ? 1 : 0;
        missing += (goBeaker == null) ? 1 : 0;
        missing += (goLiquidVol == null) ? 1 : 0;
        missing += (goTipTrigger == null) ? 1 : 0;

        if (missing == 0) DrawOk("Overall: OK (core objects found)");
        else DrawWarn($"Overall: Missing core objects = {missing} (see rows above)");
    }

    private void DrawGoRow(string label, GameObject go)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(180));
            GUI.enabled = false;
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            GUI.enabled = true;

            if (GUILayout.Button("Ping", GUILayout.Width(46))) Ping(go);
            if (GUILayout.Button("Select", GUILayout.Width(54))) Selection.activeObject = go;
        }
    }

    private void DrawKV(string k, string v)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(k, GUILayout.Width(160));
            EditorGUILayout.SelectableLabel(v ?? "(null)", GUILayout.Height(18));
        }
    }

    private void DrawBool(string label, bool ok)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = ok ? new Color(0.15f, 0.55f, 0.15f) : new Color(0.75f, 0.25f, 0.25f);
            GUILayout.Label(ok ? "✔" : "✖", GUILayout.Width(18));
            GUILayout.Label(label, style);
        }
    }

    private void DrawComponentStatus<T>(string label, GameObject go) where T : Component
    {
        bool ok = go != null && go.GetComponent<T>() != null;
        DrawBool(label, ok);
    }

    private void DrawColliderStatus(string label, GameObject go, bool mustTrigger)
    {
        bool ok = HasCollider(go, mustTrigger);
        DrawBool(label, ok);
    }

    private void DrawNonTriggerColliderExists(string label, GameObject go)
    {
        bool ok = HasNonTriggerCollider(go);
        DrawBool(label, ok);
    }

    private void DrawOk(string msg)
    {
        using (new EditorGUILayout.HorizontalScope("box"))
        {
            var s = new GUIStyle(EditorStyles.boldLabel);
            s.normal.textColor = new Color(0.15f, 0.55f, 0.15f);
            GUILayout.Label("OK", GUILayout.Width(28));
            GUILayout.Label(msg, s);
        }
    }

    private void DrawWarn(string msg)
    {
        using (new EditorGUILayout.HorizontalScope("box"))
        {
            var s = new GUIStyle(EditorStyles.boldLabel);
            s.normal.textColor = new Color(0.75f, 0.25f, 0.25f);
            GUILayout.Label("WARN", GUILayout.Width(42));
            GUILayout.Label(msg, s);
        }
    }

    private void DrawInfo(string msg)
    {
        using (new EditorGUILayout.HorizontalScope("box"))
        {
            GUILayout.Label("INFO", GUILayout.Width(36));
            GUILayout.Label(msg);
        }
    }

    private void DrawVrcCompRow(string label, GameObject go, Type tPickup, Type tSync, Type tInter)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (go == null)
            {
                DrawWarn("Object missing");
                return;
            }

            DrawBool("VRCPickup", tPickup != null && go.GetComponent(tPickup) != null);
            DrawBool("VRCObjectSync", tSync != null && go.GetComponent(tSync) != null);
            DrawBool("VRCInteractable", tInter != null && go.GetComponent(tInter) != null);
        }
    }

    // =========================================================
    // Low-level helpers
    // =========================================================
    private static void Ping(GameObject go)
    {
        if (go == null) return;
        EditorGUIUtility.PingObject(go);
    }

    private static void AddIf(List<UnityEngine.Object> list, UnityEngine.Object obj)
    {
        if (obj != null) list.Add(obj);
    }

    private static GameObject FindGO(string path)
    {
        var t = FindByPath(path);
        return t ? t.gameObject : null;
    }

    private static Transform FindByPath(string path)
    {
        var seg = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (seg.Length == 0) return null;

        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        Transform cur = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == seg[0])
            {
                cur = roots[i].transform;
                break;
            }
        }
        if (cur == null) return null;

        for (int i = 1; i < seg.Length; i++)
        {
            cur = cur.Find(seg[i]);
            if (cur == null) return null;
        }
        return cur;
    }

    private static T FindComponentInScene<T>() where T : Component
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        foreach (var c in all)
        {
            if (c == null) continue;
            var go = c.gameObject;
            if (go == null) continue;
            if (!go.scene.IsValid()) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            return c;
        }
        return null;
    }

    private static bool ArrayContains(GameObject[] arr, GameObject go)
    {
        if (arr == null || go == null) return false;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == go) return true;
        return false;
    }

    private static bool ArrayContains(UdonSharp.UdonSharpBehaviour[] arr, UdonSharp.UdonSharpBehaviour target)
    {
        if (arr == null || target == null) return false;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == target) return true;
        return false;
    }

    private static bool HasCollider(GameObject go, bool mustTrigger)
    {
        if (go == null) return false;
        var col = go.GetComponent<Collider>();
        if (col == null) return false;
        if (mustTrigger && !col.isTrigger) return false;
        return true;
    }

    private static bool HasNonTriggerCollider(GameObject go)
    {
        if (go == null) return false;
        var cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null && !cols[i].isTrigger) return true;
        return false;
    }

    private static bool HasComp<T>(GameObject go) where T : Component
    {
        return go != null && go.GetComponent<T>() != null;
    }

    private static void LogReq(string what, bool ok)
    {
        if (ok) Debug.Log("[OK] " + what);
        else Debug.LogWarning("[MISSING] " + what);
    }

    private static bool TryInvokeStaticMenu(string className, string methodName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == null) continue;
                if (t.Name != className) continue;

                var mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (mi == null) return false;

                try { mi.Invoke(null, null); return true; }
                catch (Exception e) { Debug.LogException(e); return false; }
            }
        }
        return false;
    }

    private static object GetFieldObj(object obj, string fieldName)
    {
        if (obj == null) return null;
        var t = obj.GetType();
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return null;
        try { return f.GetValue(obj); } catch { return null; }
    }

    private static void TryCall(object obj, string methodName)
    {
        if (obj == null) return;
        var t = obj.GetType();
        var mi = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi == null) return;
        if (mi.GetParameters().Length != 0) return;
        try { mi.Invoke(obj, null); } catch (Exception e) { Debug.LogException(e); }
    }

    private static bool SafeIsVR(ModeRouter router)
    {
        if (router == null) return false;
        try { return router.IsVR(); } catch { return false; }
    }

    private static bool SafeGetForcePC(ModeRouter router)
    {
        if (router == null) return false;
        var f = router.GetType().GetField("forcePC", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;
        try { return (bool)f.GetValue(router); } catch { return false; }
    }

    private static Type FindTypeInAppDomain(string fullName)
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = asm.GetType(fullName, false); } catch { }
                if (t != null) return t;
            }
        }
        catch { }
        return null;
    }
}
#endif
