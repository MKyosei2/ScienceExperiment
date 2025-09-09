using UdonSharp;
using UnityEngine;

public enum VisualPhase { GasTransparent = 0, LiquidClearWire = 1, SolidPowder = 2 }
public enum PhaseSource { InspectorSelect = 0, AutoByElement = 1 }

[AddComponentMenu("VRC Lab/Chemistry/ChemVisualController")]
public class ChemVisualController : UdonSharpBehaviour
{
    [Header("Target Animator & Parameters")]
    public Animator targetAnimator;
    public string boolIsVR = "IsVR";
    public string intPhase = "Phase";

    [Header("どこからPhaseを決めるか")]
    public PhaseSource phaseSource = PhaseSource.InspectorSelect;

    [Header("Inspectorで選ぶ既定フェーズ")]
    public VisualPhase inspectorPhasePC = VisualPhase.GasTransparent;
    public VisualPhase inspectorPhaseVR = VisualPhase.GasTransparent;

    [Header("元素ID（空なら GameObject 名）")]
    public string elementId;

    [Header("ガス粒子の表示")]
    public GameObject gasParticles;
    public bool showParticlesInPC = true;
    public bool showParticlesInVR = true;

    public void OnModePC() { SetIsVR(false); ApplyPhaseBySource(false); ApplyGasParticles(); }
    public void OnModeVR() { SetIsVR(true); ApplyPhaseBySource(true); ApplyGasParticles(); }

    private void OnEnable() { SetIsVR(false); ApplyPhaseBySource(false); ApplyGasParticles(); }

    private void SetIsVR(bool isVR) { if (targetAnimator != null) targetAnimator.SetBool(boolIsVR, isVR); }

    private void ApplyPhaseBySource(bool isVR)
    {
        int p = (int)VisualPhase.GasTransparent;
        if (phaseSource == PhaseSource.InspectorSelect)
        { p = (int)(isVR ? inspectorPhaseVR : inspectorPhasePC); }
        else
        { string id = ResolveElementId(); p = (int)AutoPhaseFromElement(id); }
        if (targetAnimator != null) targetAnimator.SetInteger(intPhase, p);
    }

    private string ResolveElementId() { if (!string.IsNullOrEmpty(elementId)) return elementId.ToUpper(); return gameObject.name != null ? gameObject.name.ToUpper() : ""; }

    private VisualPhase AutoPhaseFromElement(string id)
    {
        if (id == "H" || id == "HE" || id == "N" || id == "O" || id == "F" || id == "NE" || id == "CL" || id == "AR" || id == "KR" || id == "XE" || id == "RN")
            return VisualPhase.GasTransparent;
        return VisualPhase.SolidPowder; // デフォは固体粉
    }

    private void ApplyGasParticles()
    {
        if (gasParticles == null || targetAnimator == null) return;
        bool isVR = targetAnimator.GetBool(boolIsVR);
        int ph = targetAnimator.GetInteger(intPhase);
        bool show = (ph == (int)VisualPhase.GasTransparent) && ((isVR && showParticlesInVR) || (!isVR && showParticlesInPC));
        gasParticles.SetActive(show);
    }

    // 任意API
    public void SetPhaseGas() { SetPhase((int)VisualPhase.GasTransparent); }
    public void SetPhaseLiquidClear() { SetPhase((int)VisualPhase.LiquidClearWire); }
    public void SetPhaseSolid() { SetPhase((int)VisualPhase.SolidPowder); }
    public void SetPhase(int p) { phaseSource = PhaseSource.InspectorSelect; inspectorPhasePC = (VisualPhase)Mathf.Clamp(p, 0, 2); if (targetAnimator != null) targetAnimator.SetInteger(intPhase, p); ApplyGasParticles(); }
}
