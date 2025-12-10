using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public string command;

    public override void Interact()
    {
        if (env != null)
        {
            env.Modify(command);
        }
    }
}
