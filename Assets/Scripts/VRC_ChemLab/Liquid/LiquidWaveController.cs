using UdonSharp;
using UnityEngine;

public class LiquidWaveController : UdonSharpBehaviour
{
    public LiquidSurfaceController surface;

    public void AddWave(float strength)
    {
        if (surface != null)
            surface.SetWave(strength);
    }
}
