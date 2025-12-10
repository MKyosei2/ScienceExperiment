using UdonSharp;
using UnityEngine;

public class ConditionAdjuster : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;

    public bool isTemperature;
    public bool isHumidity;
    public bool isPressure;

    public float step = 1f;

    public override void Interact()
    {
        if (env == null) return;

        if (isTemperature)
        {
            env.AdjustTemperature(step);
        }
        else if (isHumidity)
        {
            env.AdjustHumidity(step);
        }
        else if (isPressure)
        {
            env.AdjustPressure(step);
        }
    }
}
