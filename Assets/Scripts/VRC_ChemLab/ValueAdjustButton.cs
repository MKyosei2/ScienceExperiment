using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;

    public enum AdjustType { Temperature, Humidity, Pressure }
    public AdjustType adjustType;

    public float step = 1f;

    public override void Interact()
    {
        if (env == null) return;

        switch (adjustType)
        {
            case AdjustType.Temperature:
                env.AdjustTemperature(step);
                break;

            case AdjustType.Humidity:
                env.AdjustHumidity(step);
                break;

            case AdjustType.Pressure:
                env.AdjustPressure(step);
                break;
        }
    }
}
