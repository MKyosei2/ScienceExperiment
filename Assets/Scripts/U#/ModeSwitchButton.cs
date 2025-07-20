using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ModeSwitchButton : UdonSharpBehaviour
{
    public ModeSwitcher modeSwitcher;

    public override void Interact()
    {
        if (modeSwitcher != null)
        {
            modeSwitcher.ToggleMode();
        }
    }
}
