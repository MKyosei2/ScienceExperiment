using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager environmentManager;
    public bool adjustTemperature; // true = 温度, false = 圧力
    public float step = 1f;

    public void Press()
    {
        if (environmentManager == null) return;

        if (adjustTemperature)
            environmentManager.AdjustTemperature(step);
        else
            environmentManager.AdjustPressure(step);
    }
}
