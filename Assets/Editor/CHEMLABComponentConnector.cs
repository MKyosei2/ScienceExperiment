using UnityEditor;
using UnityEngine;
using TMPro;

public class CHEMLABComponentConnector
{
    [MenuItem("CHEMLAB VR/コンポーネントのフィールドを接続")]
    public static void ConnectComponentReferences()
    {
        var holder = GameObject.Find("SelectedObjectHolder")?.GetComponent<SelectedObjectHolder>();
        var modeSwitcher = GameObject.Find("ModeSwitcher")?.GetComponent<ModeSwitcher>();
        var controller = GameObject.Find("ExperimentController")?.GetComponent<ExperimentController>();
        var startButton = GameObject.Find("ExperimentStartButton")?.GetComponent<ExperimentStartButton>();
        var modeButton = GameObject.Find("ModeSwitchButton")?.GetComponent<ModeSwitchButton>();
        var resultReceiver = GameObject.Find("ResultReceiver")?.GetComponent<ResultReceiver>();
        var experimentHistory = GameObject.Find("ExperimentHistory")?.GetComponent<ExperimentHistory>();
        var aiSender = GameObject.Find("AIRequestSender")?.GetComponent<AIRequestSender>();
        var aiHandler = GameObject.Find("AIReactionHandler")?.GetComponent<AIReactionHandler>();
        var monitor = GameObject.Find("VRExperimentMonitor")?.GetComponent<VRExperimentMonitor>();

        var modeLabelTMP = GameObject.Find("ModeLabel")?.GetComponent<TextMeshProUGUI>();
        var statusTextTMP = GameObject.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        if (modeSwitcher)
        {
            modeSwitcher.modeLabel = modeLabelTMP;
            modeSwitcher.experimentButton = GameObject.Find("ExperimentStartButton");
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

        if (startButton)
        {
            startButton.controller = controller;
            startButton.modeSwitcher = modeSwitcher;
            startButton.experimentTableRoot = GameObject.Find("ExperimentTable")?.transform;
            EditorUtility.SetDirty(startButton);
        }

        if (modeButton)
        {
            modeButton.modeSwitcher = modeSwitcher;
            EditorUtility.SetDirty(modeButton);
        }

        if (resultReceiver)
        {
            resultReceiver.holder = holder;
            resultReceiver.history = experimentHistory;
            EditorUtility.SetDirty(resultReceiver);
        }

        if (aiSender)
        {
            aiSender.monitor = monitor;
            aiSender.statusText = statusTextTMP;
            EditorUtility.SetDirty(aiSender);
        }

        var selBtns = new[] {
            GameObject.Find("ElementSelectButton")?.GetComponent<ZoneSelectionButton>(),
            GameObject.Find("ToolSelectButton")?.GetComponent<ZoneSelectionButton>(),
            GameObject.Find("ConditionSelectButton")?.GetComponent<ZoneSelectionButton>()
        };
        var selZones = new[] {
            GameObject.Find("ElementZone")?.GetComponent<SelectionZone>(),
            GameObject.Find("ToolZone")?.GetComponent<SelectionZone>(),
            GameObject.Find("ConditionZone")?.GetComponent<SelectionZone>()
        };
        for (int i = 0; i < 3; i++)
        {
            if (selBtns[i] && selZones[i])
            {
                selBtns[i].holder = holder;
                selBtns[i].selectionZone = selZones[i];
                EditorUtility.SetDirty(selBtns[i]);
            }
        }

        Debug.Log("✅ CHEMLAB VR: 参照の自動接続が完了しました。");
    }
}
