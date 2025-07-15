using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChemLabUIBuilder
{
    [MenuItem("CHEMLAB VR/Build UI Hierarchy")]
    public static void BuildUI()
    {
        GameObject uiRoot = new GameObject("UI");

        // Canvas_PC
        GameObject canvasPC = CreateCanvas("Canvas_PC", uiRoot.transform);
        CreateTMPDropdown("ElementDropdown", canvasPC.transform);
        CreateTMPDropdown("ToolDropdown", canvasPC.transform);
        CreateTMPDropdown("ConditionDropdown", canvasPC.transform);

        GameObject summaryDisplay = CreateTMPText("SelectionSummaryDisplay", canvasPC.transform,
            "@InferenceBot\nelement: \ntool: \ncondition:");

        // Canvas_VR
        GameObject canvasVR = CreateCanvas("Canvas_VR", uiRoot.transform);
        GameObject modeLabel = CreateTMPText("ModeLabel", canvasVR.transform, "🎮 VRモード");

        // モード切替ボタン（UI外部）
        GameObject switchButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
        switchButton.name = "ModeSwitchButton_3D";
        switchButton.transform.SetParent(uiRoot.transform);
        switchButton.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
        ModeSwitchButton switchButtonComp = switchButton.AddComponent<ModeSwitchButton>();

        // ModeSwitcher（または取得）
        ModeSwitcher switcher = Object.FindObjectOfType<ModeSwitcher>();
        if (switcher == null)
        {
            GameObject obj = new GameObject("ModeSwitcher_Auto");
            switcher = obj.AddComponent<ModeSwitcher>();
        }

        // 接続
        switcher.pcUIRoot = canvasPC;
        switcher.vrUIRoot = canvasVR;
        switcher.experimentButton = null; // ExperimentStartButtonは3Dで別スクリプトが管理
        if (modeLabel.TryGetComponent(out TextMeshProUGUI label))
            switcher.modeLabel = label;

        switchButtonComp.modeSwitcher = switcher;

        Selection.activeGameObject = uiRoot;
        Debug.Log("✅ UI Hierarchy を生成しました");
    }

    private static GameObject CreateCanvas(string name, Transform parent)
    {
        GameObject canvasGO = new GameObject(name);
        canvasGO.transform.SetParent(parent);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    private static GameObject CreateTMPText(string name, Transform parent, string content)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        RectTransform rt = tmp.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 100);
        return textGO;
    }

    private static GameObject CreateTMPDropdown(string name, Transform parent)
    {
        GameObject dropdownGO = new GameObject(name);
        dropdownGO.transform.SetParent(parent);
        TMP_Dropdown dropdown = dropdownGO.AddComponent<TMP_Dropdown>();
        RectTransform rt = dropdown.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 40);
        return dropdownGO;
    }
}
