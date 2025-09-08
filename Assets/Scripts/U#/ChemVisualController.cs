using UdonSharp;
using UnityEngine;

/// 気体/液体透明/固体粉の見た目を「1本」で制御する。
/// - Animator の Int "Phase" を切り替え（0:Gas, 1:LiquidClearWire, 2:SolidPowder）
/// - ModeActivation から OnModePC/OnModeVR を受け取り、Bool "IsVR" と粒子表示を同期
/// - 元素ID（任意）からの簡易オート判定 or インスペクタ選択で Phase を決定
[AddComponentMenu("VRC Lab/Chemistry/ChemVisualController")]
public class ChemVisualController : UdonSharpBehaviour
{
    public enum VisualPhase { GasTransparent = 0, LiquidClearWire = 1, SolidPowder = 2 }
    public enum PhaseSource { InspectorSelect = 0, AutoByElement = 1 }

    [Header("Target Animator & Parameters")]
    public Animator targetAnimator;
    [Tooltip("Animator Bool (VR/PCの副作用制御用)")] public string boolIsVR = "IsVR";
    [Tooltip("Animator Int  (0:Gas,1:Liquid,2:Solid)")] public string intPhase = "Phase";

    [Header("どこからPhaseを決めるか")]
    public PhaseSource phaseSource = PhaseSource.InspectorSelect;

    [Header("Inspectorで選ぶ場合の既定フェーズ")]
    public VisualPhase inspectorPhasePC = VisualPhase.GasTransparent;
    public VisualPhase inspectorPhaseVR = VisualPhase.GasTransparent;

    [Header("元素IDによる簡易オート（省略可。空ならGameObject名を参照）")]
    public string elementId; // "H", "He", "Na" など

    [Header("ガス（粒の見た目）")]
    public GameObject gasParticles;     // 粒子の親（無ければ未使用扱い）
    public bool showParticlesInPC = true;
    public bool showParticlesInVR = true;

    // ---- ModeActivation からの通知を受ける ----
    public void OnModePC()
    {
        SetIsVR(false);
        ApplyPhaseBySource(false);
        ApplyGasParticles();
    }

    public void OnModeVR()
    {
        SetIsVR(true);
        ApplyPhaseBySource(true);
        ApplyGasParticles();
    }

    // ---- 初期化（ModeActivationが無い場合でも最低限の見た目を適用）----
    private void OnEnable()
    {
        // 初回はPC想定で適用（ModeActivationがあれば直後に上書きされる）
        SetIsVR(false);
        ApplyPhaseBySource(false);
        ApplyGasParticles();
    }

    // ===== 内部処理 =====

    private void SetIsVR(bool isVR)
    {
        if (targetAnimator != null)
            targetAnimator.SetBool(boolIsVR, isVR);
    }

    private void ApplyPhaseBySource(bool isVR)
    {
        int p = (int)VisualPhase.GasTransparent;

        if (phaseSource == PhaseSource.InspectorSelect)
        {
            p = (int)(isVR ? inspectorPhaseVR : inspectorPhasePC);
        }
        else // AutoByElement
        {
            string id = ResolveElementId();
            VisualPhase auto = AutoPhaseFromElement(id);
            p = (int)auto;
        }

        if (targetAnimator != null)
            targetAnimator.SetInteger(intPhase, p);
    }

    private string ResolveElementId()
    {
        if (!string.IsNullOrEmpty(elementId)) return elementId.ToUpper();
        return gameObject.name != null ? gameObject.name.ToUpper() : "";
    }

    // 超簡易マッピング：典型的な常温相だけを網羅
    private VisualPhase AutoPhaseFromElement(string id)
    {
        // 代表的な常温気体
        if (id == "H" || id == "HE" || id == "N" || id == "O" || id == "F" || id == "NE" ||
            id == "CL" || id == "AR" || id == "KR" || id == "XE" || id == "RN")
            return VisualPhase.GasTransparent;

        // （純粋な）透明液体は稀。要件では「透明液体はワイヤ＋屈折」
        // ユーザーが明示的に Liquid を使いたい場合は InspectorSelect を使ってください。
        // ここではデフォルト Solid 扱い。
        return VisualPhase.SolidPowder;
    }

    private void ApplyGasParticles()
    {
        if (gasParticles == null || targetAnimator == null) return;

        bool isVR = targetAnimator.GetBool(boolIsVR);
        int phase = targetAnimator.GetInteger(intPhase);

        bool shouldShow = (phase == (int)VisualPhase.GasTransparent) &&
                          ((isVR && showParticlesInVR) || (!isVR && showParticlesInPC));

        gasParticles.SetActive(shouldShow);
    }

    // 任意：UI等から直接フェーズを切り替えたい場合
    public void SetPhaseGas() { SetPhase((int)VisualPhase.GasTransparent); }
    public void SetPhaseLiquidClear() { SetPhase((int)VisualPhase.LiquidClearWire); }
    public void SetPhaseSolid() { SetPhase((int)VisualPhase.SolidPowder); }
    public void SetPhase(int p)
    {
        phaseSource = PhaseSource.InspectorSelect;
        inspectorPhasePC = (VisualPhase)Mathf.Clamp(p, 0, 2);
        if (targetAnimator != null) targetAnimator.SetInteger(intPhase, p);
        ApplyGasParticles();
    }
}
