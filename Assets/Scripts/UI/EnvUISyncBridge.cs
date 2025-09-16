// Assets/Scripts/UI/EnvUISyncBridge.cs
// 3Dボタン（＋／−）で温度・湿度・圧力を増減。数値はTMP_InputFieldで直接入力（フォーカスでキーボード表示）。
// 各項目に上限/下限を設定し、超えないようクランプ。Editor内でも単独で検証可能。
// ※ 新規スクリプト作成なし：既存ファイルを丸ごとこの内容に置換してください。

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using VRC.Udon;

[DisallowMultipleComponent]
public class EnvUISyncBridge : MonoBehaviour
{
    // ====== 3D「＋／−」ボタン（Transform or GameObject） ======
    [Header("3D Buttons (assign Transforms with Colliders)")]
    public Transform tempPlus;    // 温度＋
    public Transform tempMinus;   // 温度−
    public Transform humPlus;     // 湿度＋
    public Transform humMinus;    // 湿度−
    public Transform presPlus;    // 圧力＋
    public Transform presMinus;   // 圧力−

    [Tooltip("Play時に対象TransformにBoxColliderが無ければ自動で付与（Editor専用）")]
    public bool editorAutoAddBoxCollider = true;

    // ====== 数値表示・入力（TMP_InputField） ======
    [Header("TMP_InputFields (display & input)")]
    public TMP_InputField tempInput;
    public TMP_InputField humInput;
    public TMP_InputField presInput;

    // ====== 値とステップ、上限/下限 ======
    [Header("Values (current)")]
    public float temperature = 20f;
    public float humidity = 50f;
    public float pressureAtm = 1.0f;

    [Header("Step (delta per click)")]
    public float tempStep = 1.0f;
    public float humStep = 1.0f;
    public float presStep = 0.1f;

    [Header("Hard Limits (will be clamped)")]
    public float tempMin = -273f;
    public float tempMax = 5000f;
    public float humMin = 0f;
    public float humMax = 100f;
    public float presMin = 0.0f;
    public float presMax = 100.0f;

    [Header("Display Format")]
    public string tempFormat = "0.##";
    public string humFormat = "0.##";
    public string presFormat = "0.###";

    [Header("Udon (optional)")]
    public UdonBehaviour udonTarget;
    public string tempVarName = "baseTempC";
    public string humVarName = "baseHumidity";
    public string presVarName = "basePressureAtm";
    public string applyEventName = "ApplyAll";

    [Header("Misc")]
    public bool logWarnings = false;

#if UNITY_EDITOR
    [Header("Editor Aids")]
    [Tooltip("TMP_InputField周りのUIブロッカーになり得るVRC_UiShape/Colliderを再生時に除去（Editorのみ）")]
    public bool editorPurgeUiBlockers = true;
    [Tooltip("World Space Canvas なら EventCamera を MainCamera に自動割当（Editorのみ）")]
    public bool editorSetEventCamera = true;
#endif

    Camera _cam;
    bool _isTypingTemp, _isTypingHum, _isTypingPres;

    void OnEnable()
    {
        _cam = Camera.main;

#if UNITY_EDITOR
        if (editorAutoAddBoxCollider) EnsureCollider(tempPlus);
        if (editorAutoAddBoxCollider) EnsureCollider(tempMinus);
        if (editorAutoAddBoxCollider) EnsureCollider(humPlus);
        if (editorAutoAddBoxCollider) EnsureCollider(humMinus);
        if (editorAutoAddBoxCollider) EnsureCollider(presPlus);
        if (editorAutoAddBoxCollider) EnsureCollider(presMinus);

        if (editorPurgeUiBlockers) EditorPurgeUiBlockers();
        if (editorSetEventCamera)  EditorEnsureEventCameraOnCanvas();
        EditorEnsureEventSystem();
#endif

        // TMP_InputField: 変更/確定/フォーカス
        if (tempInput)
        {
            tempInput.onValueChanged.AddListener((s) => { _isTypingTemp = true; });
            tempInput.onEndEdit.AddListener(OnTempEndEdit);
            tempInput.onSelect.AddListener(_ => TryOpenKeyboard(tempInput));
        }
        if (humInput)
        {
            humInput.onValueChanged.AddListener((s) => { _isTypingHum = true; });
            humInput.onEndEdit.AddListener(OnHumEndEdit);
            humInput.onSelect.AddListener(_ => TryOpenKeyboard(humInput));
        }
        if (presInput)
        {
            presInput.onValueChanged.AddListener((s) => { _isTypingPres = true; });
            presInput.onEndEdit.AddListener(OnPresEndEdit);
            presInput.onSelect.AddListener(_ => TryOpenKeyboard(presInput));
        }

        // 初期表示
        RefreshAllDisplays();
        PushAllToUdon();
    }

    void OnDisable()
    {
        // UnityEventsの解除（存在チェック込み）
        if (tempInput) { tempInput.onEndEdit.RemoveListener(OnTempEndEdit); tempInput.onSelect.RemoveAllListeners(); }
        if (humInput) { humInput.onEndEdit.RemoveListener(OnHumEndEdit); humInput.onSelect.RemoveAllListeners(); }
        if (presInput) { presInput.onEndEdit.RemoveListener(OnPresEndEdit); presInput.onSelect.RemoveAllListeners(); }
    }

    // ======================= クリック検出（3D・Editor） =======================
    void Update()
    {
        // Editor/Standaloneでのクリック検出
        if (Input.GetMouseButtonDown(0))
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var tr = hit.transform;

                if (Same(tr, tempPlus)) AdjustTemperature(+tempStep);
                else if (Same(tr, tempMinus)) AdjustTemperature(-tempStep);
                else if (Same(tr, humPlus)) AdjustHumidity(+humStep);
                else if (Same(tr, humMinus)) AdjustHumidity(-humStep);
                else if (Same(tr, presPlus)) AdjustPressure(+presStep);
                else if (Same(tr, presMinus)) AdjustPressure(-presStep);
            }
        }
    }

    bool Same(Transform a, Transform b)
    {
        if (!a || !b) return false;
        // 子オブジェクト（ボタン内パーツ）をクリックした場合でも親登録で拾えるよう、親方向にたどって判定
        var t = a;
        while (t != null)
        {
            if (t == b) return true;
            t = t.parent;
        }
        return false;
    }

    // ======================= 値の調整と反映 =======================
    void AdjustTemperature(float delta)
    {
        temperature = Mathf.Clamp(temperature + delta, tempMin, tempMax);
        UpdateInput(tempInput, temperature, tempFormat, ref _isTypingTemp);
        PushToUdonVar(tempVarName, temperature, alsoApplyEvent: true);
    }

    void AdjustHumidity(float delta)
    {
        humidity = Mathf.Clamp(humidity + delta, humMin, humMax);
        UpdateInput(humInput, humidity, humFormat, ref _isTypingHum);
        PushToUdonVar(humVarName, humidity, alsoApplyEvent: true);
    }

    void AdjustPressure(float delta)
    {
        pressureAtm = Mathf.Clamp(pressureAtm + delta, presMin, presMax);
        UpdateInput(presInput, pressureAtm, presFormat, ref _isTypingPres);
        PushToUdonVar(presVarName, pressureAtm, alsoApplyEvent: true);
    }

    // ======================= 入力確定（キーボード確定/Enter/フォーカス外れ） =======================
    void OnTempEndEdit(string s)
    {
        _isTypingTemp = false;
        if (!TryParse(s, out var v)) { UpdateInput(tempInput, temperature, tempFormat, ref _isTypingTemp); return; }
        temperature = Mathf.Clamp(v, tempMin, tempMax);
        UpdateInput(tempInput, temperature, tempFormat, ref _isTypingTemp);
        PushToUdonVar(tempVarName, temperature, alsoApplyEvent: true);
    }
    void OnHumEndEdit(string s)
    {
        _isTypingHum = false;
        if (!TryParse(s, out var v)) { UpdateInput(humInput, humidity, humFormat, ref _isTypingHum); return; }
        humidity = Mathf.Clamp(v, humMin, humMax);
        UpdateInput(humInput, humidity, humFormat, ref _isTypingHum);
        PushToUdonVar(humVarName, humidity, alsoApplyEvent: true);
    }
    void OnPresEndEdit(string s)
    {
        _isTypingPres = false;
        if (!TryParse(s, out var v)) { UpdateInput(presInput, pressureAtm, presFormat, ref _isTypingPres); return; }
        pressureAtm = Mathf.Clamp(v, presMin, presMax);
        UpdateInput(presInput, pressureAtm, presFormat, ref _isTypingPres);
        PushToUdonVar(presVarName, pressureAtm, alsoApplyEvent: true);
    }

    // ======================= 表示・Udon =======================
    void RefreshAllDisplays()
    {
        bool f1 = false, f2 = false, f3 = false;
        UpdateInput(tempInput, temperature, tempFormat, ref f1);
        UpdateInput(humInput, humidity, humFormat, ref f2);
        UpdateInput(presInput, pressureAtm, presFormat, ref f3);
    }

    void PushAllToUdon()
    {
        PushToUdonVar(tempVarName, temperature, alsoApplyEvent: false);
        PushToUdonVar(humVarName, humidity, alsoApplyEvent: false);
        PushToUdonVar(presVarName, pressureAtm, alsoApplyEvent: true); // 最後にまとめてApply
    }

    void UpdateInput(TMP_InputField field, float v, string fmt, ref bool isTypingFlag)
    {
        if (!field) return;
        string text = string.IsNullOrEmpty(fmt)
            ? v.ToString(CultureInfo.InvariantCulture)
            : v.ToString(fmt, CultureInfo.InvariantCulture);

        field.SetTextWithoutNotify(text);
        if (!isTypingFlag) field.caretPosition = text.Length;
    }

    void PushToUdonVar(string varName, float v, bool alsoApplyEvent)
    {
        if (!udonTarget || string.IsNullOrEmpty(varName)) return;
        udonTarget.SetProgramVariable(varName, v);
        if (alsoApplyEvent && !string.IsNullOrEmpty(applyEventName))
            udonTarget.SendCustomEvent(applyEventName);
    }

    // ======================= Utility =======================
    bool TryParse(string s, out float v)
    {
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return true;
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return true;
        v = 0f; return false;
    }

    void TryOpenKeyboard(TMP_InputField input)
    {
        if (TouchScreenKeyboard.isSupported)
        {
            TouchScreenKeyboard.Open(input ? input.text : "", TouchScreenKeyboardType.DecimalPad, false, false, false, false);
        }
    }

#if UNITY_EDITOR
    void EnsureCollider(Transform t)
    {
        if (!t) return;
        if (!t.GetComponent<Collider>()) t.gameObject.AddComponent<BoxCollider>();
    }

    void EditorPurgeUiBlockers()
    {
        // VRC_UiShapeを除去（Editorテスト時のUIブロック回避）
        var comps = GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            if (c.GetType().Name == "VRC_UiShape")
                DestroyImmediate(c);
        }
        // 入力欄近辺の不要なColliderを除去（自身配下のみ）
        foreach (var col in GetComponentsInChildren<Collider>(true))  DestroyImmediate(col);
        foreach (var col in GetComponentsInChildren<Collider2D>(true)) DestroyImmediate(col);

        // Raycaster安全設定
        var canvas = GetComponentInParent<Canvas>();
        if (canvas)
        {
            var gr = canvas.GetComponent<GraphicRaycaster>();
            if (!gr) gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }
    }

    void EditorEnsureEventCameraOnCanvas()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null && Camera.main)
            canvas.worldCamera = Camera.main;
    }

    void EditorEnsureEventSystem()
    {
        if (!FindObjectOfType<EventSystem>())
        {
            var go = new GameObject("EventSystem (Auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
#endif
}
