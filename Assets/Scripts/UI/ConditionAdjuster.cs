using UdonSharp;
using UnityEngine;

public class ConditionAdjuster : UdonSharpBehaviour
{
    public string mode;     // "temperature", "humidity", "pressure"
    public int value;       // -1 or +1
    public ChemEnvironmentManager env;

    public override void Interact()
    {
        if (env == null) return;

        if (mode == "temperature")
            env._AdjustTemperature(value);

        else if (mode == "humidity")
            env._AdjustHumidity(value);

        else if (mode == "pressure")
            env._AdjustPressure(value);
    }
}
