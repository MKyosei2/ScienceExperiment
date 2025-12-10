using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public AdjustType adjustType;
    public float step = 1f;

    public override void Interact()
    {
        if (env == null) return;

        if (adjustType == AdjustType.Temperature)
            env.AdjustTemperature(step);

        else if (adjustType == AdjustType.Humidity)
            env.AdjustHumidity(step);

        else if (adjustType == AdjustType.Pressure)
            env.AdjustPressure(step);
    }
}
