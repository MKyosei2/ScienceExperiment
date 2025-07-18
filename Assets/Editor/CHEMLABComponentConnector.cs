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

        // ✅ Hierarchy上の Element / Tool / Condition の子オブジェクトを範囲として各ゾーンに適用
        ApplyZoneFromSceneChildren("Element", "ElementExperimentZone");
        ApplyZoneFromSceneChildren("Tool", "ToolExperimentZone");
        ApplyZoneFromSceneChildren("Condition", "ConditionExperimentZone");

        // ✅ ExperimentZoneにもSelectionZoneタグを付与
        string[] expZones = new[] {
            "ElementExperimentZone",
            "ToolExperimentZone",
            "ConditionExperimentZone"
        };
        foreach (string zone in expZones)
        {
            GameObject z = GameObject.Find(zone);
            if (z != null) z.tag = "SelectionZone";
        }

        Debug.Log("🔁 CHEMLAB VR: 各ExperimentZoneのBoxColliderがHierarchyの子オブジェクトに基づき更新されました。");
    }

    private static void ApplyZoneFromSceneChildren(string parentName, string zoneName)
    {
        GameObject parent = GameObject.Find(parentName);
        if (parent == null) return;

        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
        Bounds? bounds = null;
        foreach (var r in renderers)
        {
            if (r.gameObject.name.Contains("(Clone)")) continue; // 生成されたものは除外

            if (bounds == null) bounds = r.bounds;
            else bounds = EncapsulateBounds(bounds.Value, r.bounds);
        }

        ApplyToZone(zoneName, bounds);
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
