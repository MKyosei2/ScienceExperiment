#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChemLab_OneTime_AssignAllInspectors
{
    // -------- Menu --------
    [MenuItem("Tools/ChemLab/OneTime/Assign All Inspector References (Safe)")]
    public static void AssignAll()
    {
        // Build caches (inactive objects included)
        var cache = new SceneCache();
        cache.Build();

        int assignedCount = 0;
        int arrayAssignedCount = 0;
        int touchedComponents = 0;

        // Iterate components in loaded scenes (including inactive)
        Component[] allComponents = cache.AllComponents;
        for (int i = 0; i < allComponents.Length; i++)
        {
            var comp = allComponents[i];
            if (!cache.IsInLoadedScene(comp)) continue;

            Type t = comp.GetType();
            // Skip editor-only / internal
            if (t == typeof(Transform) || t == typeof(RectTransform)) continue;

            bool anyChange = false;

            // Fill serialized object references
            anyChange |= FillSerializedObjectReferences(comp, cache, ref assignedCount, ref arrayAssignedCount);

            if (anyChange)
            {
                touchedComponents++;
                EditorUtility.SetDirty(comp);
            }
        }

        // Post-pass: ensure ElementEffectAnchor exists under each tool (if a ChemElementSpawner exists)
        EnsureToolAnchors(cache);

        // Save scenes that are dirty
        SaveAllDirtyScenes();

        Debug.Log($"[ChemLab OneTime] Assigned refs: {assignedCount}, arrays: {arrayAssignedCount}, touched components: {touchedComponents}. Saved dirty scenes.");
    }

    // -------- Core filling logic --------
    private static bool FillSerializedObjectReferences(Component comp, SceneCache cache, ref int assignedCount, ref int arrayAssignedCount)
    {
        bool changed = false;
        Type t = comp.GetType();

        // Walk fields including base types up to MonoBehaviour
        for (Type cur = t; cur != null && cur != typeof(MonoBehaviour); cur = cur.BaseType)
        {
            FieldInfo[] fields = cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var f in fields)
            {
                if (f.IsStatic || f.IsInitOnly) continue;
                if (Attribute.IsDefined(f, typeof(NonSerializedAttribute))) continue;

                bool isPublic = f.IsPublic;
                bool hasSerializeField = Attribute.IsDefined(f, typeof(SerializeField));
                if (!isPublic && !hasSerializeField) continue;

                // Avoid writing hidden runtime/debug state
                if (Attribute.IsDefined(f, typeof(HideInInspector))) continue;

                Type ft = f.FieldType;

                // Handle arrays of UnityEngine.Object
                if (ft.IsArray)
                {
                    Type et = ft.GetElementType();
                    if (et == null || !typeof(UnityEngine.Object).IsAssignableFrom(et)) continue;

                    var arr = f.GetValue(comp) as Array;
                    if (arr != null && arr.Length > 0) continue; // don't overwrite

                    var candidates = FindArrayCandidates(comp, cache, f.Name, et);
                    if (candidates != null && candidates.Length > 0)
                    {
                        Array newArr = Array.CreateInstance(et, candidates.Length);
                        for (int i = 0; i < candidates.Length; i++) newArr.SetValue(candidates[i], i);
                        Undo.RecordObject(comp, "Assign Inspector Array");
                        f.SetValue(comp, newArr);
                        arrayAssignedCount++;
                        changed = true;
                    }
                    continue;
                }

                // Handle single UnityEngine.Object references
                if (!typeof(UnityEngine.Object).IsAssignableFrom(ft)) continue;

                var current = f.GetValue(comp) as UnityEngine.Object;
                if (current != null) continue; // don't overwrite

                UnityEngine.Object found = FindBestCandidate(comp, cache, f.Name, ft);
                if (found != null)
                {
                    Undo.RecordObject(comp, "Assign Inspector Reference");
                    f.SetValue(comp, found);
                    assignedCount++;
                    changed = true;
                }
            }
        }

        return changed;
    }

    // -------- Candidate selection --------
    private static UnityEngine.Object FindBestCandidate(Component owner, SceneCache cache, string fieldName, Type wantedType)
    {
        // 1) Name-based preferred targets (strong hints)
        var preferredNames = cache.GetPreferredNamesForField(fieldName, wantedType);
        if (preferredNames != null)
        {
            for (int i = 0; i < preferredNames.Length; i++)
            {
                var byName = cache.FindByName(preferredNames[i], wantedType, owner.gameObject.scene);
                if (byName != null) return byName;
            }
        }

        // 2) Try same GameObject / children
        if (wantedType == typeof(Transform))
        {
            // If it's a Transform field, often they intend a named child
            Transform child = FindChildByApproxName(owner.transform, fieldName, 3);
            if (child != null) return child;
        }
        else if (typeof(Component).IsAssignableFrom(wantedType))
        {
            var same = owner.GetComponent(wantedType);
            if (same != null) return same;

            var child = owner.GetComponentInChildren(wantedType, true);
            if (child != null) return child;
        }

        // 3) Global: find all objects of this type in the same loaded scene and choose best
        var all = cache.FindAllOfType(wantedType, owner.gameObject.scene);
        if (all == null || all.Length == 0) return null;

        // Prefer closest in hierarchy: same root name / under same top-level parent
        Transform ownerRoot = owner.transform.root;
        UnityEngine.Object best = null;
        int bestScore = int.MinValue;

        string fieldNorm = Normalize(fieldName);

        for (int i = 0; i < all.Length; i++)
        {
            var obj = all[i];
            if (obj == null) continue;

            GameObject go = GetGameObject(obj);
            if (go == null) continue;

            int score = 0;

            // same scene already ensured by cache
            if (go.transform.root == ownerRoot) score += 50;

            string nameNorm = Normalize(go.name);

            // match by name similarity with field name
            if (!string.IsNullOrEmpty(fieldNorm) && nameNorm.Contains(fieldNorm)) score += 25;

            // prefer specific ChemLab hub nodes
            if (nameNorm.Contains("CHEM")) score += 10;
            if (nameNorm.Contains("SYSTEM")) score += 5;
            if (nameNorm.Contains("WORLD")) score += 3;
            if (nameNorm.Contains("UI")) score += 3;

            // prefer active objects (editor-wise)
            if (go.activeInHierarchy) score += 2;

            if (score > bestScore)
            {
                bestScore = score;
                best = obj;
            }
        }

        return best;
    }

    private static UnityEngine.Object[] FindArrayCandidates(Component owner, SceneCache cache, string fieldName, Type elementType)
    {
        // For Renderer[] / Collider[] / ParticleSystem[] etc, first try local children.
        if (typeof(Component).IsAssignableFrom(elementType))
        {
            // Specific common arrays
            if (elementType == typeof(Renderer))
                return owner.GetComponentsInChildren<Renderer>(true);

            if (elementType == typeof(ParticleSystem))
                return owner.GetComponentsInChildren<ParticleSystem>(true);

            if (elementType == typeof(Collider))
                return owner.GetComponentsInChildren<Collider>(true);

            // Generic
            var comps = owner.GetComponentsInChildren(elementType, true);
            if (comps != null && comps.Length > 0)
            {
                UnityEngine.Object[] objs = new UnityEngine.Object[comps.Length];
                for (int i = 0; i < comps.Length; i++) objs[i] = comps[i];
                return objs;
            }
        }

        // If local didn't work, global by type (same scene)
        var all = cache.FindAllOfType(elementType, owner.gameObject.scene);
        return all;
    }

    private static Transform FindChildByApproxName(Transform root, string approx, int depth)
    {
        if (root == null || depth < 0) return null;
        string target = Normalize(approx);
        int c = root.childCount;
        for (int i = 0; i < c; i++)
        {
            var ch = root.GetChild(i);
            if (Normalize(ch.name).Contains(target))
                return ch;
        }
        if (depth == 0) return null;
        for (int i = 0; i < c; i++)
        {
            var found = FindChildByApproxName(root.GetChild(i), approx, depth - 1);
            if (found != null) return found;
        }
        return null;
    }

    // -------- Post-pass: anchors --------
    private static void EnsureToolAnchors(SceneCache cache)
    {
        // Find ChemElementSpawner instances (by type name to avoid hard dependency if scripts renamed)
        var spawners = cache.FindAllByTypeName("ChemElementSpawner");
        if (spawners == null || spawners.Length == 0) return;

        for (int si = 0; si < spawners.Length; si++)
        {
            var spawner = spawners[si] as Component;
            if (spawner == null) continue;

            // Try read fields via reflection
            var toolRoot = GetFieldValue<Transform>(spawner, "toolModelsRoot");
            var anchorName = GetFieldValue<string>(spawner, "elementEffectAnchorName");
            if (string.IsNullOrEmpty(anchorName)) anchorName = "ElementEffectAnchor";

            if (toolRoot == null) continue;

            int c = toolRoot.childCount;
            for (int i = 0; i < c; i++)
            {
                Transform tool = toolRoot.GetChild(i);
                if (tool == null) continue;

                // Only create anchor if tool has some visible geometry under it (to avoid creating under empty groups)
                bool hasRenderer = tool.GetComponentInChildren<Renderer>(true) != null;
                if (!hasRenderer) continue;

                Transform existing = tool.Find(anchorName);
                if (existing != null) continue;

                Undo.RegisterFullObjectHierarchyUndo(tool.gameObject, "Create ElementEffectAnchor");
                var anchorGo = new GameObject(anchorName);
                anchorGo.transform.SetParent(tool, false);
                anchorGo.transform.localPosition = Vector3.zero;
                anchorGo.transform.localRotation = Quaternion.identity;
                anchorGo.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(tool);
            }
        }
    }

    // -------- Saving --------
    private static void SaveAllDirtyScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.IsValid() || !s.isLoaded) continue;
            if (s.isDirty)
            {
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
            }
        }
    }

    // -------- Helpers / Cache --------
    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim().ToUpperInvariant();
        s = s.Replace(" ", "").Replace("_", "").Replace("-", "");
        return s;
    }

    private static GameObject GetGameObject(UnityEngine.Object o)
    {
        if (o == null) return null;
        if (o is GameObject go) return go;
        if (o is Component c) return c.gameObject;
        return null;
    }

    private static T GetFieldValue<T>(Component comp, string fieldName)
    {
        if (comp == null) return default;
        Type t = comp.GetType();
        for (Type cur = t; cur != null && cur != typeof(MonoBehaviour); cur = cur.BaseType)
        {
            var f = cur.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) continue;
            object v = f.GetValue(comp);
            if (v is T tv) return tv;
            return default;
        }
        return default;
    }

    private sealed class SceneCache
    {
        public Component[] AllComponents;
        private Dictionary<string, List<UnityEngine.Object>> byName = new Dictionary<string, List<UnityEngine.Object>>();
        private Dictionary<Type, List<UnityEngine.Object>> byType = new Dictionary<Type, List<UnityEngine.Object>>();
        private Dictionary<string, Type> typeNameToType = new Dictionary<string, Type>();

        public void Build()
        {
            // Build list of components including inactive objects in loaded scenes
            // Resources.FindObjectsOfTypeAll returns assets too; filter by scene validity later.
            AllComponents = Resources.FindObjectsOfTypeAll<Component>();

            // Cache all transforms & all components by name (for name lookup)
            for (int i = 0; i < AllComponents.Length; i++)
            {
                var c = AllComponents[i];
                if (c == null) continue;
                if (!IsInLoadedScene(c)) continue;

                AddByName(c.gameObject.name, c.gameObject);
                AddByName(c.gameObject.name, c.transform);
                AddByName(c.GetType().Name, c);

                // byType
                AddByType(c.GetType(), c);
                // map type name
                if (!typeNameToType.ContainsKey(c.GetType().Name))
                    typeNameToType[c.GetType().Name] = c.GetType();
            }
        }

        public bool IsInLoadedScene(Component c)
        {
            if (c == null) return false;
            var go = c.gameObject;
            if (go == null) return false;
            var s = go.scene;
            return s.IsValid() && s.isLoaded && !EditorUtility.IsPersistent(go);
        }

        private void AddByName(string name, UnityEngine.Object obj)
        {
            if (string.IsNullOrEmpty(name) || obj == null) return;
            string key = name.Trim();
            if (!byName.TryGetValue(key, out var list))
            {
                list = new List<UnityEngine.Object>();
                byName[key] = list;
            }
            list.Add(obj);
        }

        private void AddByType(Type t, UnityEngine.Object obj)
        {
            if (t == null || obj == null) return;
            if (!byType.TryGetValue(t, out var list))
            {
                list = new List<UnityEngine.Object>();
                byType[t] = list;
            }
            list.Add(obj);
        }

        public UnityEngine.Object FindByName(string name, Type wantedType, Scene scene)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!byName.TryGetValue(name, out var list)) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                if (obj == null) continue;
                if (!wantedType.IsAssignableFrom(obj.GetType()))
                {
                    // allow Component when wanted is GameObject/Transform and vice versa
                    if (wantedType == typeof(GameObject))
                    {
                        var go = GetGameObject(obj);
                        if (go != null && go.scene == scene) return go;
                    }
                    if (wantedType == typeof(Transform))
                    {
                        if (obj is Transform tr && tr.gameObject.scene == scene) return tr;
                        var go = GetGameObject(obj);
                        if (go != null && go.scene == scene) return go.transform;
                    }
                    continue;
                }

                // Ensure in same scene
                var go2 = GetGameObject(obj);
                if (go2 != null && go2.scene == scene) return obj;
            }
            return null;
        }

        public UnityEngine.Object[] FindAllOfType(Type wantedType, Scene scene)
        {
            if (wantedType == null) return null;

            // direct type list
            if (byType.TryGetValue(wantedType, out var list))
            {
                return FilterByScene(list, scene);
            }

            // Otherwise, gather assignable types
            List<UnityEngine.Object> gathered = new List<UnityEngine.Object>();
            foreach (var kv in byType)
            {
                if (wantedType.IsAssignableFrom(kv.Key))
                    gathered.AddRange(kv.Value);
            }
            if (gathered.Count == 0) return null;
            return FilterByScene(gathered, scene);
        }

        private UnityEngine.Object[] FilterByScene(List<UnityEngine.Object> list, Scene scene)
        {
            List<UnityEngine.Object> outList = new List<UnityEngine.Object>();
            for (int i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                if (obj == null) continue;
                var go = GetGameObject(obj);
                if (go != null && go.scene == scene) outList.Add(obj);
            }
            return outList.ToArray();
        }

        public UnityEngine.Object[] FindAllByTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            if (!typeNameToType.TryGetValue(typeName, out var t))
            {
                // Try resolve from all loaded assemblies
                t = Type.GetType(typeName);
                if (t == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = asm.GetType(typeName);
                        if (t != null) break;
                    }
                }
                if (t == null) return null;
                typeNameToType[typeName] = t;
            }

            if (!byType.TryGetValue(t, out var list)) return null;
            // No scene filter here (caller can filter)
            return list.ToArray();
        }

        public string[] GetPreferredNamesForField(string fieldName, Type wantedType)
        {
            string fn = Normalize(fieldName);

            // Known ChemLab wiring (strong hints)
            // Databases / managers / orchestrator
            if (fn.Contains("ELEMENTDB") && wantedType.Name.Contains("ChemElementDatabase")) return new[] { "ChemElementDatabase" };
            if (fn.Contains("REACTIONDB") && wantedType.Name.Contains("ChemicalReactionDatabase")) return new[] { "ChemicalReactionDatabase" };
            if (fn.Contains("PREDICTOR")) return new[] { "ReactionPredictor" };
            if (fn.Contains("EXPLAINGENERATOR")) return new[] { "ChemExplainGenerator" };
            if (fn.Contains("ENVIRONMENT") || fn.Contains("ENVMANAGER")) return new[] { "ChemEnvironmentManager" };
            if (fn.Contains("ORCHESTRATOR")) return new[] { "ExperimentOrchestrator" };
            if (fn.Contains("AIREQUEST") || fn == "AI") return new[] { "AIRequestSender" };
            if (fn.Contains("STATUSDISPLAY")) return new[] { "Status", "ChemStatusDisplay" };
            if (fn.Contains("SAMPLEVISUAL") || fn.Contains("VISUAL")) return new[] { "SampleVisual" };
            if (fn.Contains("REACTIONANIMATOR") || fn.Contains("VFX")) return new[] { "ReactionVFX", "LiquidReaction" };

            // 3D roots
            if (fn.Contains("TOOLMODELSROOT")) return new[] { "Tool", "Tools", "VR_Props" };
            if (fn.Contains("ELEMENTMODELSROOT")) return new[] { "Element", "Elements" };

            // placement
            if (fn.Contains("CONTAINERTRANSFORM")) return new[] { "BeakerPoint", "PourTargetPoint", "ExperimentTable", "LabOrigin" };
            if (fn.Contains("HEATSOURCE")) return new[] { "BurnerPoint", "HeatSource", "Gasburner", "Burner" };

            // UI texts (best-effort; will still try by type if not found)
            if (wantedType.Name.Contains("TextMeshPro") && fn.Contains("STATUSTEXT")) return new[] { "StatusText", "Status" };
            if (wantedType.Name == "Text" && fn.Contains("HINT")) return new[] { "HintText", "Hint" };
            if (wantedType.Name == "Text" && fn.Contains("EXPLAIN")) return new[] { "ExplainText", "Explain" };
            if (wantedType.Name == "Text" && fn.Contains("SAFETY")) return new[] { "SafetyText", "Safety" };
            if (wantedType.Name == "Text" && fn.Contains("DEBUG")) return new[] { "DebugText", "Debug" };

            return null;
        }
    }
}
#endif