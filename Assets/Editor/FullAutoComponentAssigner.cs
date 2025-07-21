using UnityEditor;
using UnityEngine;
using TMPro;
using VRC.Udon;
using UdonSharp;
using System;

public class FullAutoComponentAssigner : EditorWindow
{
    [MenuItem("ChemLab VR/🔁 全スクリプト＋Inspector割り当て")]
    public static void ShowWindow()
    {
        GetWindow<FullAutoComponentAssigner>("ChemLab 自動構成");
    }

    void OnGUI()
    {
        if (GUILayout.Button("⚙️ Hierarchy全体に割り当て実行"))
        {
            AssignAll();
        }
    }

    static void AssignAll()
    {
        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        GameObject holderObj = GameObject.Find("SelectedObjectHolder");
        var holder = holderObj ? holderObj.GetComponent<SelectedObjectHolder>() : null;

        foreach (GameObject root in allObjects)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject obj = child.gameObject;
                string objName = obj.name.ToLower();

                AddIfMissing<PlaceableObject>(obj);

                // =====================
                // ゾーン処理
                // =====================
                if (objName.Contains("zone"))
                {
                    if (objName.Contains("experiment"))
                        AddIfMissing<SelectionZone>(obj);
                    else
                        AddIfMissing<ZoneTrigger>(obj);
                }

                // =====================
                // RoomAsset分類
                // =====================
                if (IsInPath(obj, "RoomAsset/Element"))
                {
                    AddIfMissing<ElementSelector>(obj);
                    AddIfMissing<SelectorObject>(obj)?.SetObjectType("Element");
                    AddIfMissing<ZoneAwareObject>(obj);
                }

                if (IsInPath(obj, "RoomAsset/Tool"))
                {
                    AddIfMissing<ToolSelector>(obj);
                    AddIfMissing<SelectorObject>(obj)?.SetObjectType("Tool");
                    AddIfMissing<ZoneAwareObject>(obj);
                }

                if (IsInPath(obj, "RoomAsset/Condition"))
                {
                    AddIfMissing<ConditionSelector>(obj);
                    AddIfMissing<SelectorObject>(obj)?.SetObjectType("Condition");
                    AddIfMissing<ZoneAwareObject>(obj);
                }

                // =====================
                // 名称で判定するリンク設定
                // =====================
                if (obj.name == "ModeSwitcher")
                {
                    var switcher = AddIfMissing<ModeSwitcher>(obj);
                    if (switcher.modeLabel == null)
                    {
                        var label = GameObject.Find("ModeLabel")?.GetComponent<TextMeshProUGUI>();
                        if (label) switcher.modeLabel = label;
                    }

                    if (switcher.experimentButton == null)
                    {
                        var button = GameObject.Find("ExperimentStartButton");
                        if (button) switcher.experimentButton = button;
                    }
                }

                if (obj.name == "ExperimentStartButton")
                {
                    var expBtn = AddIfMissing<ExperimentStartButton>(obj);
                    if (expBtn.statusTextUI == null)
                    {
                        var status = GameObject.Find("StatusText");
                        if (status) expBtn.statusTextUI = status.GetComponent<UdonBehaviour>();
                    }
                    if (expBtn.experimentController == null)
                    {
                        var ctrl = GameObject.Find("ExperimentController");
                        if (ctrl) expBtn.experimentController = ctrl.GetComponent<UdonBehaviour>();
                    }
                }

                if (obj.name == "StatusText")
                {
                    var status = AddIfMissing<StatusTextUI>(obj);
                    if (status.statusText == null)
                    {
                        var text = obj.GetComponentInChildren<TextMeshProUGUI>();
                        if (text) status.statusText = text;
                    }
                }

                if (obj.name == "SelectionSummaryDisplay")
                {
                    var summary = AddIfMissing<SelectionSummaryDisplay>(obj);
                    if (summary.holder == null && holder) summary.holder = holder;
                }

                if (obj.name == "ResultReceiver")
                {
                    var receiver = AddIfMissing<ResultReceiver>(obj);
                    if (receiver.holder == null && holder) receiver.holder = holder;
                    if (receiver.history == null)
                    {
                        var historyObj = GameObject.Find("ExperimentHistory");
                        if (historyObj) receiver.history = historyObj.GetComponent<ExperimentHistory>();
                    }

                    var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (var t in texts)
                    {
                        if (receiver.resultText == null && t.name.ToLower().Contains("result"))
                            receiver.resultText = t;
                        else if (receiver.triviaText == null && t.name.ToLower().Contains("trivia"))
                            receiver.triviaText = t;
                    }
                }

                AddByExactName<VRExperimentMonitor>(obj, "VRExperimentMonitor");
                AddByExactName<ExperimentHistory>(obj, "ExperimentHistory");
                AddByExactName<ExperimentController>(obj, "ExperimentController");
                AddByExactName<AIRequestSender>(obj, "AIRequestSender");
                AddByExactName<ExperimentTableTrigger>(obj, "ExperimentTable");
                AddByExactName<SelectedObjectHolder>(obj, "SelectedObjectHolder");
            }
        }

        Debug.Log("✅ 全スクリプトとInspector設定を自動割り当てしました！");
    }

    static T AddIfMissing<T>(GameObject obj) where T : Component
    {
        T existing = obj.GetComponent<T>();
        if (existing == null)
        {
            Undo.AddComponent<T>(obj);
            return obj.GetComponent<T>();
        }
        return existing;
    }

    static void AddByExactName<T>(GameObject obj, string name) where T : Component
    {
        if (obj.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            AddIfMissing<T>(obj);
    }

    static bool IsInPath(GameObject obj, string expectedPath)
    {
        string path = GetHierarchyPath(obj.transform);
        return path.Replace(" ", "").ToLower().Contains(expectedPath.ToLower().Replace(" ", ""));
    }

    static string GetHierarchyPath(Transform obj)
    {
        string path = obj.name;
        while (obj.parent != null)
        {
            obj = obj.parent;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
