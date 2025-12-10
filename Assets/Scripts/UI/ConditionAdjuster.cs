using UdonSharp;
using UnityEngine;

public class ConditionAdjuster : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public string command; // TempPlus / TempMinus / HumPlus ...

    public override void Interact()
    {
        if (env != null)
        {
            env.Modify(command);
        }
    }
}
