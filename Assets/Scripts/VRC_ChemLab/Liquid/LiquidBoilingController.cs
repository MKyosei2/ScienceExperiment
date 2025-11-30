using UdonSharp;
using UnityEngine;

public class LiquidBoilingController : UdonSharpBehaviour
{
    public LiquidSurfaceController surface;

    public void PlayBoil()
    {
        if (surface != null)
            surface.SetRipple(0.5f);
    }

    public void PlayHeat()
    {
        if (surface != null)
            surface.PulseColor(new Color(1f, 0.4f, 0f, 1f));
    }
}
