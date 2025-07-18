#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class CHEMLABComponentConnector
{
    [MenuItem("CHEMLAB VR/Hierarchyを再接続する")]
    public static void Reconnect()
    {
        var holder = GameObject.Find("SelectedObjectHolder")?.GetComponent<SelectedObjectHolder>();
        var modeSwitcher = GameObject.Find("ModeSwitcher")?.GetComponent<ModeSwitcher>();
        var controller = GameObject.Find("ExperimentController")?.GetComponent<ExperimentController>();
        var aiSender = GameObject.Find("AIRequestSender")?.GetComponent<AIRequestSender>();
        var vrMonitor = GameObject.Find("VRExperimentMonitor")?.GetComponent<VRExperimentMonitor>();
        var history = GameObject.Find("ExperimentHistory")?.GetComponent<ExperimentHistory>();
        var resultReceiver = GameObject.Find("ResultReceiver")?.GetComponent<ResultReceiver>();

        var modeLabelTMP = GameObject.Find("ModeLabel")?.GetComponent<TMPro.TextMeshProUGUI>();
        var statusTextTMP = GameObject.Find("StatusText")?.GetComponent<TMPro.TextMeshProUGUI>();

        var startBtn = GameObject.Find("ExperimentStartButton")?.GetComponent<ExperimentStartButton>();
        var modeBtn = GameObject.Find("ModeSwitchButton")?.GetComponent<ModeSwitchButton>();

        var elementBtn = GameObject.Find("ElementSelectButton")?.GetComponent<ZoneSelectionButton>();
        var toolBtn = GameObject.Find("ToolSelectButton")?.GetComponent<ZoneSelectionButton>();
        var conditionBtn = GameObject.Find("ConditionSelectButton")?.GetComponent<ZoneSelectionButton>();

        var elementZone = GameObject.Find("ElementZone")?.GetComponent<SelectionZone>();
        var toolZone = GameObject.Find("ToolZone")?.GetComponent<SelectionZone>();
        var conditionZone = GameObject.Find("ConditionZone")?.GetComponent<SelectionZone>();

        if (modeSwitcher)
        {
            modeSwitcher.modeLabel = modeLabelTMP;
            modeSwitcher.experimentButton = startBtn?.gameObject;
            modeSwitcher.pcUIRoot = GameObject.Find("SelectionButtons");
            modeSwitcher.vrUIRoot = GameObject.Find("Zones");
            EditorUtility.SetDirty(modeSwitcher);
        }

        if (controller)
        {
            controller.holder = holder;
            controller.requestSender = aiSender;
            EditorUtility.SetDirty(controller);
        }

        if (startBtn)
        {
            startBtn.controller = controller;
            startBtn.modeSwitcher = modeSwitcher;
            startBtn.experimentTableRoot = GameObject.Find("ExperimentTable")?.transform;
            EditorUtility.SetDirty(startBtn);
        }

        if (modeBtn)
        {
            modeBtn.modeSwitcher = modeSwitcher;
            EditorUtility.SetDirty(modeBtn);
        }

        if (resultReceiver)
        {
            resultReceiver.holder = holder;
            resultReceiver.history = history;
            EditorUtility.SetDirty(resultReceiver);
        }

        if (aiSender)
        {
            aiSender.monitor = vrMonitor;
            aiSender.statusText = statusTextTMP;
            EditorUtility.SetDirty(aiSender);
        }

        if (elementBtn && elementZone)
        {
            elementBtn.holder = holder;
            elementBtn.selectionZone = elementZone;
            EditorUtility.SetDirty(elementBtn);
        }
        if (toolBtn && toolZone)
        {
            toolBtn.holder = holder;
            toolBtn.selectionZone = toolZone;
            EditorUtility.SetDirty(toolBtn);
        }
        if (conditionBtn && conditionZone)
        {
            conditionBtn.holder = holder;
            conditionBtn.selectionZone = conditionZone;
            EditorUtility.SetDirty(conditionBtn);
        }

        // 🧠 各カテゴリ別に ExperimentZone を自動リサイズ
        string prefabRoot = "Assets/Prefab/RoomAsset";
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabRoot });
        Dictionary<string, Bounds?> zoneBounds = new Dictionary<string, Bounds?>()
        {
            { "Element", null },
            { "Tool", null },
            { "Condition", null }
        };

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                string category = GetCategoryName(prefab.name);
                if (category != null)
                {
                    Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        if (zoneBounds[category] == null) zoneBounds[category] = r.bounds;
                        else zoneBounds[category] = EncapsulateBounds(zoneBounds[category].Value, r.bounds);
                    }
                }
                GameObject.DestroyImmediate(temp);
            }
        }

        ApplyToZone("ElementExperimentZone", zoneBounds["Element"]);
        ApplyToZone("ToolExperimentZone", zoneBounds["Tool"]);
        ApplyToZone("ConditionExperimentZone", zoneBounds["Condition"]);

        Debug.Log("🔁 CHEMLAB VR: 参照とゾーンBoxColliderの再接続が完了しました。");
    }

    private static string GetCategoryName(string prefabName)
    {
        prefabName = prefabName.ToLower();
        if (prefabName.Contains("element")) return "Element";
        if (prefabName.Contains("tool")) return "Tool";
        if (prefabName.Contains("condition")) return "Condition";
        return null;
    }

    private static void ApplyToZone(string zoneName, Bounds? bounds)
    {
        if (!bounds.HasValue) return;
        GameObject z = GameObject.Find(zoneName);
        if (z != null)
        {
            var col = z.GetComponent<BoxCollider>();
            if (col != null)
            {
                z.transform.position = bounds.Value.center;
                col.size = bounds.Value.size;
                EditorUtility.SetDirty(z);
            }
        }
    }

    private static Bounds EncapsulateBounds(Bounds baseBounds, Bounds newBounds)
    {
        baseBounds.Encapsulate(newBounds.min);
        baseBounds.Encapsulate(newBounds.max);
        return baseBounds;
    }
}
#endif
