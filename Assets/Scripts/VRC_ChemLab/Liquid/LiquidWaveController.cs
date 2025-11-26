using UdonSharp;

public class LiquidWaveController : UdonSharpBehaviour
{
    public LiquidSurfaceController surface;

    public void PlayWave()
    {
        SendCustomEventDelayedSeconds("_DoWave", 0f);
    }

    public void _DoWave()
    {
        float t = 0.3f;
        surface.SetWave(0.3f);
        SendCustomEventDelayedSeconds("_End", 0.5f);
    }

    public void _End()
    {
        surface.SetWave(0.0f);
    }
}
