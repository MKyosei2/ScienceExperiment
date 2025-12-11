using UdonSharp;
using UnityEngine;

public class ConditionAdjuster : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public string command; // Modify に渡す

    public override void Interact()
    {
        env.Modify(command);
    }
}
