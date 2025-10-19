using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentOrchestrator : UdonSharpBehaviour
{
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;
    public ModeRouter modeRouter;
    public VRExperimentMonitor monitor;

    private bool running = false;

    public void StartExperiment()
    {
        if (running) return;

        if (modeRouter && modeRouter.IsVR())
        {
            running = true;
            if (monitor) monitor.Log("VRモード: 手動操作で開始してください。");
            return;
        }

        if (elementSpawner)
        {
            elementSpawner.StartExperiment();
            running = true;
            if (monitor) monitor.Log("PCモード: 実験開始。");
        }
    }

    public void OnVRGestureStart()
    {
        if (!modeRouter || !modeRouter.IsVR() || running) return;

        if (elementSpawner)
        {
            elementSpawner.StartExperiment();
            running = true;
            if (monitor) monitor.Log("VRモード: ジェスチャーで実験開始。");
        }
    }

    public void ResetExperiment()
    {
        running = false;
        if (elementSpawner) elementSpawner.ResetExperiment();
        if (environmentManager) environmentManager.ResetToDefaultsAndSync();
        if (monitor) monitor.Log("リセット完了。");
    }

    public void ToggleMode()
    {
        if (modeRouter) modeRouter.Toggle();
    }
}
