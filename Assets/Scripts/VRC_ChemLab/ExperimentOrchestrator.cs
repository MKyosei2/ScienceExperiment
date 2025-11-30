using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("VRC Lab/ExperimentOrchestrator")]
public class ExperimentOrchestrator : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager environmentManager;
    public EnvUISyncBridge uiSync;

    private bool isVR;

    void Start()
    {
        isVR = Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR();
        Debug.Log($"[Orchestrator] モード: {(isVR ? "VR" : "PC")}");
    }

    public void _StartExperiment()
    {
        if (isVR)
            Debug.Log("[Orchestrator] VRモード: 手動操作で開始");
        else
            spawner.SendCustomEvent("_StartExperiment");
    }

    public void _ResetExperiment()
    {
        spawner.SendCustomEvent("_ResetExperiment");
        environmentManager.SendCustomEvent("_ResetToDefaults");
        uiSync.SendCustomEvent("_RefreshAllDisplays");
        Debug.Log("[Orchestrator] 実験リセット完了");
    }
}
