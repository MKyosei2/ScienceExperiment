using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// ExperimentOrchestrator
/// 教材フローの入口（ミッション/開始/リセット）
/// ※Importer/Hierarchyは無視して、コード側の骨格だけ提供。
/// </summary>
[AddComponentMenu("VRC Lab/ExperimentOrchestrator")]
public class ExperimentOrchestrator : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager environmentManager;
    public EnvUISyncBridge uiSync;

    [Header("Auto Start")]
    public bool autoStartOnDesktop = false;

    private void Start()
    {
        if (spawner == null) return;

        bool isVR = Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR();
        if (!isVR && autoStartOnDesktop)
        {
            spawner.SendCustomEvent("_StartExperiment");
        }
    }

    public void _StartExperiment()
    {
        if (spawner != null) spawner.SendCustomEvent("_StartExperiment");
    }

    public void _ResetExperiment()
    {
        if (spawner != null) spawner.SendCustomEvent("_ResetExperiment");
        if (environmentManager != null) environmentManager.SendCustomEvent("_ResetToDefaults");
        if (uiSync != null) uiSync.SendCustomEvent("_RefreshAllDisplays");
    }

    public void _ReleaseOperator()
    {
        if (spawner != null) spawner.SendCustomEvent("_ReleaseOperator");
    }
}
