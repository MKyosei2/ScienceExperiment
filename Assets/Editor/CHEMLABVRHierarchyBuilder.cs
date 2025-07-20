using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using VRC.SDK3.Components;
using System;
using System.Collections.Generic;

public class CHEMLABVRHierarchyBuilder : EditorWindow
{
    private static readonly Dictionary<string, Type[]> componentMap = new()
    {
        { "ModeSwitcher", new[] { typeof(BoxCollider), typeof(ModeSwitcher) } },
        { "AIRequestSender", new[] { typeof(BoxCollider), typeof(AIRequestSender) } },
        { "ResultReceiver", new[] { typeof(BoxCollider), typeof(ResultReceiver) } },
        { "ExperimentController", new[] { typeof(BoxCollider), typeof(ExperimentController) } },
        { "VRExperimentMonitor", new[] { typeof(BoxCollider), typeof(VRExperimentMonitor) } },
        { "AIReactionHandler", new[] { typeof(BoxCollider), typeof(AIReactionHandler) } },
        { "ExperimentHistory", new[] { typeof(BoxCollider), typeof(ExperimentHistory) } },
        { "ExperimentStartButton", new[] { typeof(BoxCollider), typeof(ExperimentStartButton) } },
        { "SelectedObjectHolder", new[] { typeof(BoxCollider), typeof(SelectedObjectHolder) } },
        { "ModeSwitchButton", new[] { typeof(BoxCollider), typeof(ModeSwitchButton) } },
        { "ElementZone", new[] { typeof(BoxCollider), typeof(SelectionZone) } },
        { "ToolZone", new[] { typeof(BoxCollider), typeof(SelectionZone) } },
        { "ConditionZone", new[] { typeof(BoxCollider), typeof(SelectionZone) } },
        { "ElementExperimentZone", new[] { typeof(BoxCollider), typeof(ZoneAwareObject) } },
        { "ToolExperimentZone", new[] { typeof(BoxCollider), typeof(ZoneAwareObject) } },
        { "ConditionExperimentZone", new[] { typeof(BoxCollider), typeof(ZoneAwareObject) } },
        { "Element", new[] { typeof(CanvasRenderer), typeof(CategoryDisplayManager) } },
        { "Condition", new[] { typeof(CanvasRenderer), typeof(CategoryDisplayManager) } },
        { "Tool", new[] { typeof(CanvasRenderer), typeof(CategoryDisplayManager) } },
        { "ModeLabel", new[] { typeof(RectTransform), typeof(TextMeshProUGUI) } },
        { "StatusText", new[] { typeof(RectTransform), typeof(TextMeshProUGUI) } },
        { "ExperimentTable", new[] { typeof(BoxCollider), typeof(ExperimentTableTrigger) } },
        { "Floor", new[] { typeof(BoxCollider) } },
        { "Wall", new[] { typeof(BoxCollider) } }
    };

    [MenuItem("CHEMLAB VR/Auto Build Hierarchy")]
    static void BuildHierarchy()
    {
        CreateRoot("Managers", new[] {
            "ModeSwitcher", "AIRequestSender", "ResultReceiver", "ExperimentController",
            "VRExperimentMonitor", "AIReactionHandler", "ExperimentHistory", "ExperimentStartButton", "SelectedObjectHolder"
        });

        CreateRoot("UI", new[] {
            "Element", "Condition", "Tool", "ModeLabel", "StatusText"
        });

        GameObject roomAsset = CreateGO("RoomAsset");
        var condition = LoadAndScalePrefab("Assets/Prefab/RoomAssets/Condition.prefab", roomAsset.transform, 0.5f);
        var element = LoadAndScalePrefab("Assets/Prefab/RoomAssets/Element.prefab", roomAsset.transform, 0.25f);
        var tool = LoadAndScalePrefab("Assets/Prefab/RoomAssets/Tool.prefab", roomAsset.transform, 0.5f);

        // Set transform positions
        condition.transform.localPosition = new Vector3(4.5f, 0.98f, 0);
        condition.transform.localRotation = Quaternion.identity;

        element.transform.localPosition = new Vector3(0, 2f, -4.5f);
        element.transform.localRotation = Quaternion.identity;

        tool.transform.localPosition = new Vector3(4.5f, 2.79f, 0);
        tool.transform.localRotation = Quaternion.Euler(0, 90f, 0);

        CreateRoot("Zones", new[] {
            "ElementZone", "ToolZone", "ConditionZone",
            "ElementExperimentZone", "ToolExperimentZone", "ConditionExperimentZone"
        });

        CreateRoot("SelectionButtons", new[] {
            "ExperimentStartButton", "ModeSwitchButton"
        });

        var table = CreateGO("ExperimentTable");
        AddComponents(table, "ExperimentTable");

        CreatePrimitive("Floor", PrimitiveType.Plane, new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        CreatePrimitive("Wall", PrimitiveType.Cube, new Vector3(0, 2.5f, -5), new Vector3(10, 5, 1));
        CreatePrimitive("Wall", PrimitiveType.Cube, new Vector3(0, 2.5f, 5), new Vector3(10, 5, 1));
        CreatePrimitive("Wall", PrimitiveType.Cube, new Vector3(-5, 2.5f, 0), new Vector3(1, 5, 10));
        CreatePrimitive("Wall", PrimitiveType.Cube, new Vector3(5, 2.5f, 0), new Vector3(1, 5, 10));

        FitColliderToTarget("ElementExperimentZone", element);
        FitColliderToTarget("ToolExperimentZone", tool);
        FitColliderToTarget("ConditionExperimentZone", condition);

        if (GameObject.Find("Main Camera") == null)
        {
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0, 1.6f, -7);
        }

        if (GameObject.Find("Directional Light") == null)
        {
            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("CHEMLAB VR full hierarchy and environment setup complete.");
    }

    static GameObject LoadAndScalePrefab(string assetPath, Transform parent, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.name = prefab.name;
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }
        else
        {
            Debug.LogWarning($"Prefab not found at path: {assetPath}");
            return null;
        }
    }

    static GameObject LoadPrefab(string assetPath, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.name = prefab.name;
            return instance;
        }
        else
        {
            Debug.LogWarning($"Prefab not found at path: {assetPath}");
            return null;
        }
    }

    static void FitColliderToTarget(string zoneName, GameObject target)
    {
        GameObject zone = GameObject.Find(zoneName);
        if (zone != null && target != null)
        {
            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                BoxCollider collider = zone.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.center = zone.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size;
                }
            }
        }
    }

    static void CreateRoot(string rootName, string[] children)
    {
        GameObject root = CreateGO(rootName);
        AddComponents(root, rootName);
        CreateChildren(root, children);
    }

    static GameObject CreateGO(string name, Transform parent = null)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        return go;
    }

    static void CreateChildren(GameObject parent, string[] names, params System.Type[] extraComponents)
    {
        foreach (string name in names)
        {
            GameObject child = CreateGO(name, parent.transform);
            AddComponents(child, name);
            AddComponents(child, extraComponents);
        }
    }

    static void AddComponents(GameObject go, string key)
    {
        if (componentMap.TryGetValue(key, out Type[] types))
        {
            AddComponents(go, types);
        }
    }

    static void AddComponents(GameObject go, params Type[] components)
    {
        foreach (var comp in components)
        {
            if (go.GetComponent(comp) == null)
            {
                go.AddComponent(comp);
            }
        }
    }

    static void CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        AddComponents(go, name);
    }
}