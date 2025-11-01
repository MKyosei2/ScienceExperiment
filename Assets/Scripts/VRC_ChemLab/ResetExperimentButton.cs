using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/ResetExperimentButton")]
public class ResetExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemEnvironmentManager envManager;
    public EnvUISyncBridge uiSync;

    public override void Interact()
    {
        _OnClick();
    }

    public void _OnClick()
    {
        if (spawner != null)
            spawner.SendCustomEvent("_ResetExperiment");

        if (envManager != null)
            envManager.SendCustomEvent("_ResetToDefaults");

        if (uiSync != null)
            uiSync.SendCustomEvent("_RefreshAllDisplays");
    }
}
