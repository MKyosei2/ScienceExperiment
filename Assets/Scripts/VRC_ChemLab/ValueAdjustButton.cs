using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    [Header("参照設定")]
    public ChemEnvironmentManager environmentManager;

    [Header("調整パラメータ")]
    public bool adjustTemperature;
    public bool adjustHumidity;
    public bool adjustPressure;
    public float step = 1f;

    public override void Interact()
    {
        if (environmentManager == null) return;

        if (adjustTemperature)
            environmentManager.AdjustTemperature(step);

        if (adjustHumidity)
            environmentManager.AdjustHumidity(step);

        if (adjustPressure)
            environmentManager.AdjustPressure(step);
    }
}
