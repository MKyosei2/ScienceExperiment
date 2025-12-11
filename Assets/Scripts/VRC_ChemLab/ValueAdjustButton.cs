using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public string command; // "TempUp" "TempDown" "HumUp" "HumDown" "PresUp" "PresDown"
    public ChemStatusDisplay statusDisplay;

    public override void Interact()
    {
        env.Modify(command);
        statusDisplay.RefreshUI();
    }
}
