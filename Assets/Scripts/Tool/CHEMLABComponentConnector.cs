using UnityEditor;
using UnityEngine;
using TMPro;

public class CHEMLABComponentConnector
{
    [MenuItem("CHEMLAB VR/コンポーネントのフィールドを接続")]
    public static void ConnectComponentReferences()
    {
        // 参照先を取得
        var holder = GameObject.Find("SelectedObjectHolder")?.GetComponent<SelectedObjectHolder>();
        var modeSwitcher = GameObject.Find("ModeSwitcher")?.GetComponent<ModeSwitcher>();
        var controller = GameObject.Find("ExperimentController")?.GetComponent<ExperimentController>();
        var startButton = GameObject.Find("ExperimentStartButton")?.GetComponent<ExperimentStartButton>();
        var modeButton = GameObject.Find("ModeSwitchButton")?.GetComponent<ModeSwitchButton>();
        var selectionButtons = new[]
        {
            GameObject.Find("ElementSelectButton")?.GetComponent<ZoneSelectionButton>(),
            GameObject.Find("ToolSelectButton")?.GetComponent<ZoneSelectionButton>(),
            GameObject.Find("ConditionSelectButton")?.GetComponent<ZoneSelectionButton>()
        };
        var zones = new[]
        {
            GameObject.Find("ElementZone")?.GetComponent<SelectionZone>(),
            GameObject.Find("ToolZone")?.GetComponent<SelectionZone>(),
            GameObject.Find("ConditionZone")?.GetComponent<SelectionZone>()
        };

        var resultReceiver = GameObject.Find("ResultReceiver")?.GetComponent<ResultReceiver>();
        var experimentHistory = GameObject.Find("ExperimentHistory")?.GetComponent<ExperimentHistory>();
        var aiSender = GameObject.Find("AIRequestSender")?.GetComponent<AIRequestSender>();
        var aiHandler = GameObject.Find("AIReactionHandler")?.GetComponent<AIReactionHandler>();
        var modeLabel = GameObject.Find("ModeLabel")?.GetComponent<TextMeshProUGUI>();

        // ModeSwitcher の UI参照
        if (modeSwitcher)
        {
            modeSwitcher.modeLabel = modeLabel;
            modeSwitcher.experimentButton = GameObject.Find("ExperimentStartButton");
            modeSwitcher.pcUIRoot = GameObject.Find("SelectionButtons");
            modeSwitcher.vrUIRoot = GameObject.Find("Zones");
            EditorUtility.SetDirty(modeSwitcher);
        }

        // ExperimentController の参照
        if (controller)
        {
            controller.holder = holder;
            controller.requestSender = aiSender;
            EditorUtility.SetDirty(controller);
        }

        // StartButton に ModeSwitcher, Controller を割り当て
        if (startButton)
        {
            startButton.controller = controller;
            startButton.modeSwitcher = modeSwitcher;
            EditorUtility.SetDirty(startButton);
        }

        // ModeSwitchButton に ModeSwitcher を割り当て
        if (modeButton)
        {
            modeButton.modeSwitcher = modeSwitcher;
            EditorUtility.SetDirty(modeButton);
        }

        // 各 ZoneSelectionButton に holder と zone 割当
        for (int i = 0; i < 3; i++)
        {
            if (selectionButtons[i] && zones[i])
            {
                selectionButtons[i].holder = holder;
                selectionButtons[i].selectionZone = zones[i];
                EditorUtility.SetDirty(selectionButtons[i]);
            }
        }

        // ResultReceiver の履歴・Holder 接続
        if (resultReceiver)
        {
            resultReceiver.history = experimentHistory;
            resultReceiver.holder = holder;
            EditorUtility.SetDirty(resultReceiver);
        }

        // AIRequestSender に handler 接続
        if (aiSender && aiHandler)
        {
            aiSender.handler = aiHandler;
            EditorUtility.SetDirty(aiSender);
        }

        Debug.Log("✅ CHEMLAB VR: 参照の自動接続が完了しました。");
    }
}
