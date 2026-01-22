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
        // Auto-wire missing references (scene inspector links may be lost)
        if (spawner == null)
        {
            GameObject g = GameObject.Find("ChemElementSpawner");
            if (g != null) spawner = g.GetComponent<ChemElementSpawner>();
        }
        if (envManager == null)
        {
            GameObject g2 = GameObject.Find("ChemEnvironmentManager");
            if (g2 != null) envManager = g2.GetComponent<ChemEnvironmentManager>();
        }
        if (uiSync == null)
        {
            GameObject g3 = GameObject.Find("EnvUISyncBridge");
            if (g3 != null) uiSync = g3.GetComponent<EnvUISyncBridge>();
        }

        if (spawner != null)
            spawner.SendCustomEvent("_ResetExperiment");

        if (envManager != null)
            envManager.SendCustomEvent("_ResetToDefaults");

        if (uiSync != null)
            uiSync.SendCustomEvent("_RefreshAllDisplays");
    }
}
