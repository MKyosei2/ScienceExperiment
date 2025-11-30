using UdonSharp;
using UnityEngine;

public class ModeToggleButton : UdonSharpBehaviour
{
    public ModeRouter modeRouter;

    public void Press()
    {
        if (modeRouter != null)
            modeRouter.Toggle();
    }
}
