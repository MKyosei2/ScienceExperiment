#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VRC_ChemLab_AutoWireInspector : EditorWindow
{
    private bool overwriteExisting = false;
    private bool alsoPopulateAnimatorPartsIfEmpty = true;
    private bool alsoPopulateUIIfEmpty = true;
    private bool searchInactive = true;

    [MenuItem("Tools/VRC ChemLab/Auto Wire Inspector (Modified Scripts)")]
    public static void Open()
    {
        var w = GetWindow<VRC_ChemLab_AutoWireInspector>("ChemLab AutoWire");
        w.minSize = new Vector2(420, 260);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Auto-wire (only modified scripts)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans open scenes (and optionally selected prefabs) and reassigns references for:\n" +
            "ChemElementSpawner, AIRequestSender, ChemReactionAnimator, ChemVisualController, ChemElementDatabase, ReactionPredictor.\n\n" +
            "It tries to preserve existing assignments unless 'Overwrite existing' is enabled.",
            MessageType.Info);

        overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing (force reassign)", overwriteExisting);
        alsoPopulateAnimatorPartsIfEmpty = EditorGUILayout.ToggleLeft("Populate ReactionAnimator particle/render lists if empty", alsoPopulateAnimatorPartsIfEmpty);
        alsoPopulateUIIfEmpty = EditorGUILayout.ToggleLeft("Auto-find UI Text fields if empty", alsoPopulateUIIfEmpty);
        searchInactive = EditorGUILayout.ToggleLeft("Include inactive objects", searchInactive);

        GUILayout.Space(8);

        if (GUILayout.Button("Auto-wire in OPEN SCENES", GUILayout.Height(32)))
        {
            AutoWireOpenScenes();
        }

        if (GUILayout.Button("Auto-wire in SELECTED PREFABS (Project view)", GUILayout.Height(28)))
        {
            AutoWireSelectedPrefabs();
        }

        GUILayout.Space(6);
        EditorGUILayout.HelpBox("Tip: Run this after script replacement. Then check a few key objects manually.", MessageType.None);
    }

    private void AutoWireOpenScenes()
    {
        int totalChanged = 0;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            totalChanged += AutoWireInRoots(roots, scene.name);
        }

        if (totalChanged > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ChemLab AutoWire] Done. Changed components: {totalChanged}");
    }

    private void AutoWireSelectedPrefabs()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0)
        {
            Debug.LogWarning("[ChemLab AutoWire] No selection.");
            return;
        }

        int totalChanged = 0;

        foreach (var obj in objs)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null) continue;

            // Instantiate preview to edit safely
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
            try
            {
                totalChanged += AutoWireInRoots(new[] { instance }, $"Prefab:{prefabRoot.name}");

                if (totalChanged > 0)
                {
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                }
            }
            finally
            {
                DestroyImmediate(instance);
            }
        }

        if (totalChanged > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ChemLab AutoWire] Done (prefabs). Changed components: {totalChanged}");
    }

    private int AutoWireInRoots(GameObject[] roots, string scopeLabel)
    {
        int changed = 0;

        // Find global singletons (best-effort)
        var allSpawners = FindAllInRoots<ChemElementSpawner>(roots);
        var allAIs = FindAllInRoots<AIRequestSender>(roots);
        var allDBs = FindAllInRoots<ChemElementDatabase>(roots);
        var allPredictors = FindAllInRoots<ReactionPredictor>(roots);
        var allEnvs = FindAllInRoots<ChemEnvironmentManager>(roots);

        var db = PickBestDB(allDBs);
        var predictor = PickBestPredictor(allPredictors);
        var env = PickBestEnv(allEnvs);
        var ai = PickBestAI(allAIs);

        // 1) AIRequestSender wiring
        foreach (var a in allAIs)
        {
            if (a == null) continue;
            Undo.RecordObject(a, "ChemLab AutoWire AIRequestSender");

            bool did = false;
            did |= AssignIfNeeded(a, "elementDb", db);
            did |= AssignIfNeeded(a, "reactionPredictor", predictor);

            if (did)
            {
                EditorUtility.SetDirty(a);
                changed++;
            }
        }

        // 2) Spawner wiring
        foreach (var sp in allSpawners)
        {
            if (sp == null) continue;
            Undo.RecordObject(sp, "ChemLab AutoWire ChemElementSpawner");

            bool did = false;

            did |= AssignIfNeeded(sp, "elementDb", db);
            did |= AssignIfNeeded(sp, "environment", env);
            did |= AssignIfNeeded(sp, "ai", ai);

            // Visuals: prefer children under spawner
            var visual = sp.sampleVisual != null ? sp.sampleVisual : FindNearChild<ChemVisualController>(sp.gameObject);
            var anim = sp.reactionAnimator != null ? sp.reactionAnimator : FindNearChild<ChemReactionAnimator>(sp.gameObject);

            did |= AssignIfNeeded(sp, "sampleVisual", visual);
            did |= AssignIfNeeded(sp, "reactionAnimator", anim);

            // heatSource: find by name heuristic if empty
            if ((overwriteExisting || sp.heatSource == null))
            {
                var hs = FindHeatSource(sp.gameObject);
                if (hs != null)
                {
                    sp.heatSource = hs;
                    did = true;
                }
            }

            // containerTransform: if empty, set to self
            if ((overwriteExisting || sp.containerTransform == null))
            {
                sp.containerTransform = sp.transform;
                did = true;
            }

            // Optional UI auto-wire by names
            if (alsoPopulateUIIfEmpty)
            {
                did |= AutoWireUIText(sp);
            }

            if (did)
            {
                EditorUtility.SetDirty(sp);
                changed++;
            }

            // Also populate animator parts (optional)
            if (alsoPopulateAnimatorPartsIfEmpty && anim != null)
            {
                changed += AutoPopulateReactionAnimator(anim) ? 1 : 0;
            }

            // Also populate visual parts (Solid/Liquid/Gas, renderers)
            if (visual != null)
            {
                changed += AutoPopulateVisual(visual) ? 1 : 0;
            }
        }

        Debug.Log($"[ChemLab AutoWire] Scope={scopeLabel} | Spawners={allSpawners.Length}, AIs={allAIs.Length}, DBs={allDBs.Length}, Predictors={allPredictors.Length}, Envs={allEnvs.Length} | Changed={changed}");
        return changed;
    }

    // ---------- Helpers ----------
    private T[] FindAllInRoots<T>(GameObject[] roots) where T : Component
    {
        return roots
            .SelectMany(r => r.GetComponentsInChildren<T>(searchInactive))
            .ToArray();
    }

    private ChemElementDatabase PickBestDB(ChemElementDatabase[] dbs)
    {
        if (dbs == null || dbs.Length == 0) return null;
        // Prefer the one with data populated
        var best = dbs
            .OrderByDescending(d => d != null && d.Symbols != null ? d.Symbols.Length : 0)
            .FirstOrDefault();
        return best;
    }

    private ReactionPredictor PickBestPredictor(ReactionPredictor[] ps)
    {
        if (ps == null || ps.Length == 0) return null;
        return ps[0];
    }

    private ChemEnvironmentManager PickBestEnv(ChemEnvironmentManager[] envs)
    {
        if (envs == null || envs.Length == 0) return null;
        // Prefer name containing "Env" or "Environment"
        var named = envs.FirstOrDefault(e => e != null && (e.name.ToLower().Contains("env") || e.name.ToLower().Contains("environment")));
        return named != null ? named : envs[0];
    }

    private AIRequestSender PickBestAI(AIRequestSender[] ais)
    {
        if (ais == null || ais.Length == 0) return null;
        // Prefer one that already has predictor set
        var best = ais.FirstOrDefault(a => a != null && a.reactionPredictor != null);
        return best != null ? best : ais[0];
    }

    private T FindNearChild<T>(GameObject root) where T : Component
    {
        if (root == null) return null;
        var c = root.GetComponentInChildren<T>(searchInactive);
        return c;
    }

    private Transform FindHeatSource(GameObject root)
    {
        if (root == null) return null;

        // 1) child named HeatSource
        var t = root.transform.Find("HeatSource");
        if (t != null) return t;

        // 2) any object containing "Burner" or "Heat"
        var all = root.GetComponentsInChildren<Transform>(searchInactive);
        foreach (var x in all)
        {
            if (x == null) continue;
            var n = x.name.ToLower();
            if (n.Contains("heatsource") || n.Contains("burner") || n.Contains("gasburner") || n.Contains("heat"))
                return x;
        }

        // 3) global search in scene (fallback)
        var globals = Object.FindObjectsOfType<Transform>(searchInactive);
        foreach (var x in globals)
        {
            var n = x.name.ToLower();
            if (n.Contains("heatsource") || n.Contains("burner") || n.Contains("gasburner"))
                return x;
        }

        return null;
    }

    private bool AssignIfNeeded(Object obj, string fieldName, Object value)
    {
        if (obj == null) return false;

        var so = new SerializedObject(obj);
        var sp = so.FindProperty(fieldName);
        if (sp == null) return false;

        if (!overwriteExisting && sp.objectReferenceValue != null) return false;
        if (value == null) return false;

        sp.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private bool AutoWireUIText(ChemElementSpawner sp)
    {
        if (sp == null) return false;
        bool did = false;

        // Search by common names under spawner
        if ((overwriteExisting || sp.hintText == null))
        {
            var t = FindTextByName(sp.transform, "HintText");
            if (t == null) t = FindTextByName(sp.transform, "Hint");
            if (t != null) { sp.hintText = t; did = true; }
        }

        if ((overwriteExisting || sp.explainText == null))
        {
            var t = FindTextByName(sp.transform, "ExplainText");
            if (t == null) t = FindTextByName(sp.transform, "Explain");
            if (t != null) { sp.explainText = t; did = true; }
        }

        if ((overwriteExisting || sp.safetyText == null))
        {
            var t = FindTextByName(sp.transform, "SafetyText");
            if (t == null) t = FindTextByName(sp.transform, "Safety");
            if (t != null) { sp.safetyText = t; did = true; }
        }

        if ((overwriteExisting || sp.debugText == null))
        {
            var t = FindTextByName(sp.transform, "DebugText");
            if (t == null) t = FindTextByName(sp.transform, "Debug");
            if (t != null) { sp.debugText = t; did = true; }
        }

        if (did)
        {
            Undo.RecordObject(sp, "ChemLab AutoWire UI");
            EditorUtility.SetDirty(sp);
        }

        return did;
    }

    private Text FindTextByName(Transform root, string nameContains)
    {
        if (root == null) return null;
        var all = root.GetComponentsInChildren<Text>(searchInactive);
        foreach (var t in all)
        {
            if (t == null) continue;
            if (t.name.ToLower().Contains(nameContains.ToLower())) return t;
        }
        return null;
    }

    private bool AutoPopulateReactionAnimator(ChemReactionAnimator anim)
    {
        if (anim == null) return false;

        Undo.RecordObject(anim, "ChemLab AutoPopulate ChemReactionAnimator");
        bool did = false;

        // Particles: name-based
        if ((overwriteExisting || anim.foamParticles == null))
        {
            var p = FindParticle(anim.transform, "Foam");
            if (p != null) { anim.foamParticles = p; did = true; }
        }
        if ((overwriteExisting || anim.smokeParticles == null))
        {
            var p = FindParticle(anim.transform, "Smoke");
            if (p != null) { anim.smokeParticles = p; did = true; }
        }
        if ((overwriteExisting || anim.sparkParticles == null))
        {
            var p = FindParticle(anim.transform, "Spark");
            if (p != null) { anim.sparkParticles = p; did = true; }
        }

        // Renderers arrays: fill only if empty/null
        if (alsoPopulateAnimatorPartsIfEmpty)
        {
            if ((overwriteExisting || anim.glowRenderers == null || anim.glowRenderers.Length == 0))
            {
                var rs = FindRenderersWithProperty(anim.transform, anim.emissionProperty);
                if (rs.Length > 0) { anim.glowRenderers = rs; did = true; }
            }
            if ((overwriteExisting || anim.heatRenderers == null || anim.heatRenderers.Length == 0))
            {
                var rs = FindRenderersWithProperty(anim.transform, anim.heatProperty);
                if (rs.Length > 0) { anim.heatRenderers = rs; did = true; }
            }
            if ((overwriteExisting || anim.waveRenderers == null || anim.waveRenderers.Length == 0))
            {
                var rs = FindRenderersWithProperty(anim.transform, anim.waveProperty);
                if (rs.Length > 0) { anim.waveRenderers = rs; did = true; }
            }
        }

        if (did)
        {
            EditorUtility.SetDirty(anim);
        }

        return did;
    }

    private ParticleSystem FindParticle(Transform root, string nameContains)
    {
        var ps = root.GetComponentsInChildren<ParticleSystem>(searchInactive);
        foreach (var p in ps)
        {
            if (p == null) continue;
            if (p.name.ToLower().Contains(nameContains.ToLower())) return p;
        }
        return ps.FirstOrDefault();
    }

    private Renderer[] FindRenderersWithProperty(Transform root, string property)
    {
        if (string.IsNullOrEmpty(property)) return new Renderer[0];

        var rs = root.GetComponentsInChildren<Renderer>(searchInactive);
        return rs.Where(r =>
        {
            if (r == null) return false;
            var m = r.sharedMaterial;
            if (m == null) return false;
            return m.HasProperty(property);
        }).Distinct().ToArray();
    }

    private bool AutoPopulateVisual(ChemVisualController vis)
    {
        if (vis == null) return false;

        Undo.RecordObject(vis, "ChemLab AutoPopulate ChemVisualController");
        bool did = false;

        if ((overwriteExisting || vis.solidObj == null))
        {
            var t = vis.transform.Find("Solid");
            if (t != null) { vis.solidObj = t.gameObject; did = true; }
        }
        if ((overwriteExisting || vis.liquidObj == null))
        {
            var t = vis.transform.Find("Liquid");
            if (t != null) { vis.liquidObj = t.gameObject; did = true; }
        }
        if ((overwriteExisting || vis.gasObj == null))
        {
            var t = vis.transform.Find("Gas");
            if (t != null) { vis.gasObj = t.gameObject; did = true; }
        }

        if ((overwriteExisting || vis.targetRenderers == null || vis.targetRenderers.Length == 0))
        {
            vis.targetRenderers = vis.GetComponentsInChildren<Renderer>(true);
            if (vis.targetRenderers != null && vis.targetRenderers.Length > 0) did = true;
        }

        if (did) EditorUtility.SetDirty(vis);
        return did;
    }
}
#endif
