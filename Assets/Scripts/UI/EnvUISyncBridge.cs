// Assets/Scripts/UI/EnvUISyncBridge.cs
// +/− ボタンで値を増減 ＋ TMP_InputField で直接入力（フォーカス時にソフトキーボード）
// Udon 変数更新 & カスタムイベント送信は維持

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Globalization;
using VRC.Udon; // UdonBehaviour

[DisallowMultipleComponent]
public class EnvUISyncBridge : MonoBehaviour
{
    [Header("UI")]
    public Button plusButton;           // ＋ボタン
    public Button minusButton;          // −ボタン
    public TMP_InputField input;        // 数値表示＆入力（キーボードはここで出す）

    [Header("Value")]
    public float value = 0f;            // 現在値（初期値）
    public float step = 1f;             // ＋/− の増減幅
    public bool clampInput = true;
    public float minValue = 0f;
    public float maxValue = 100f;

    [Header("Display")]
    [Tooltip("例: 0, 0.0, 0.00, 0.##。空ならそのまま表示")]
    public string format = "0.##";
    [Tooltip("入力中（onValueChanged）に表示だけ更新するか")]
    public bool liveWhileTyping = true;

    [Header("Udon target (optional)")]
    public UdonBehaviour udonTarget;            // 例: ChemEnvironmentManager
    public string udonVariableName = "baseTempC";
    public string applyEventName = "ApplyAll";

    [Header("Diagnostics")]
    public bool logWarnings = false;

#if UNITY_EDITOR
    [Header("Editor Aids (Play中のみ)")]
    [Tooltip("このオブジェクト配下の VRC_UiShape/Collider を除去（エディタ検証の邪魔を回避）")]
    public bool editorPurgeUiShapeAndColliders = true;
#endif

    bool _wired;
    bool _isTyping;
    bool _internalUpdating;

    void Reset()
    {
        if (!input) input = GetComponent<TMP_InputField>();
    }

    void OnEnable()
    {
        // 参照チェック
        if (!input)
        {
            if (logWarnings) Debug.LogWarning("[EnvUISyncBridge] Input is not assigned.", this);
            return;
        }

#if UNITY_EDITOR
        if (editorPurgeUiShapeAndColliders) EditorPurgeUiBlockers();
#endif

        Wire();

        // 初期値を整えて表示
        SetValue(value, pushToUI: true, pushToUdon: true);
    }

    void OnDisable()
    {
        Unwire();
    }

    void Wire()
    {
        if (_wired) return;

        if (plusButton) plusButton.onClick.AddListener(OnPlus);
        if (minusButton) minusButton.onClick.AddListener(OnMinus);

        input.onEndEdit.AddListener(OnInputEndEdit);
        if (liveWhileTyping) input.onValueChanged.AddListener(OnInputTyping);
        input.onSelect.AddListener(OpenSoftKeyboardOnSelect);

        // 最低限のフォーカス妨害回避
        input.interactable = true;
        var cg = input.GetComponentInParent<CanvasGroup>();
        if (cg) { cg.interactable = true; cg.blocksRaycasts = true; }

        // World Space Canvas の基本（推奨）
        var canvas = input.GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
        {
            var cam = Camera.main;
            if (cam) canvas.worldCamera = cam;
        }

        _wired = true;
    }

    void Unwire()
    {
        if (!_wired) return;

        if (plusButton) plusButton.onClick.RemoveListener(OnPlus);
        if (minusButton) minusButton.onClick.RemoveListener(OnMinus);

        input.onEndEdit.RemoveListener(OnInputEndEdit);
        input.onValueChanged.RemoveListener(OnInputTyping);
        input.onSelect.RemoveListener(OpenSoftKeyboardOnSelect);

        _wired = false;
    }

    // ===== ボタン操作 =====
    void OnPlus()
    {
        SetValue(value + step, pushToUI: true, pushToUdon: true);
    }

    void OnMinus()
    {
        SetValue(value - step, pushToUI: true, pushToUdon: true);
    }

    // ===== 入力欄 =====
    void OnInputTyping(string s)
    {
        if (_internalUpdating) return;
        _isTyping = true;

        // 入力途中は許容（-, ., -., ""）
        if (string.IsNullOrEmpty(s) || s == "-" || s == "." || s == "-.") return;

        if (TryParse(s, out var v))
        {
            if (clampInput) v = Mathf.Clamp(v, minValue, maxValue);
            // 入力中は表示だけ整える（Udonには確定時のみ送る）
            value = v;
        }
    }

    void OnInputEndEdit(string s)
    {
        _isTyping = false;

        if (!TryParse(s, out var v))
        {
            // パース不可：現在値で書き戻す
            UpdateInputText(value);
            return;
        }

        SetValue(v, pushToUI: true, pushToUdon: true);
    }

    // ===== 値のセット共通処理 =====
    void SetValue(float v, bool pushToUI, bool pushToUdon)
    {
        if (clampInput) v = Mathf.Clamp(v, minValue, maxValue);
        value = v;

        if (pushToUI) UpdateInputText(v);
        if (pushToUdon) PushToUdon(v);
    }

    void UpdateInputText(float v)
    {
        if (!input) return;

        var text = string.IsNullOrEmpty(format)
            ? v.ToString(CultureInfo.InvariantCulture)
            : v.ToString(format, CultureInfo.InvariantCulture);

        _internalUpdating = true;
        input.SetTextWithoutNotify(text);
        if (!_isTyping) input.caretPosition = text.Length;
        _internalUpdating = false;
    }

    // ===== Udon 反映 =====
    void PushToUdon(float v)
    {
        if (!udonTarget) return;
        if (string.IsNullOrEmpty(udonVariableName))
        {
            if (logWarnings) Debug.LogWarning("[EnvUISyncBridge] udonVariableName is empty.", this);
            return;
        }

        udonTarget.SetProgramVariable(udonVariableName, v);
        if (!string.IsNullOrEmpty(applyEventName))
            udonTarget.SendCustomEvent(applyEventName);
    }

    // ===== キーボード =====
    void OpenSoftKeyboardOnSelect(string _) => TryOpenSoftKeyboard();

    void TryOpenSoftKeyboard()
    {
        if (TouchScreenKeyboard.isSupported)
        {
            var t = input ? input.text : "";
            TouchScreenKeyboard.Open(t, TouchScreenKeyboardType.DecimalPad, false, false, false, false);
        }
    }

    // ===== Utils =====
    bool TryParse(string s, out float v)
    {
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return true;
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return true;
        v = 0f; return false;
    }

#if UNITY_EDITOR
    // Editor だけ：VRC_UiShape 由来の BoxCollider 等でブロックされないよう排除
    void EditorPurgeUiBlockers()
    {
        // VRC_UiShape を型名で検出して削除
        var comps = GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            if (c.GetType().Name == "VRC_UiShape")
                DestroyImmediate(c);
        }
        // Collider 類も除去
        foreach (var col in GetComponentsInChildren<Collider>(true))  DestroyImmediate(col);
        foreach (var col in GetComponentsInChildren<Collider2D>(true)) DestroyImmediate(col);

        // Canvas の Raycaster を安全側へ
        var canvas = GetComponentInParent<Canvas>();
        if (canvas)
        {
            var gr = canvas.GetComponent<GraphicRaycaster>();
            if (!gr) gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null && Camera.main)
                canvas.worldCamera = Camera.main;
        }

        // EventSystem が無ければ作成
        if (!FindObjectOfType<EventSystem>())
        {
            var go = new GameObject("EventSystem (Auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
#endif
}
