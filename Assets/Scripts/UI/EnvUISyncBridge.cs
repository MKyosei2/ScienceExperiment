using UdonSharp;
using UnityEngine;

public class EnvUISyncBridge : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public ChemStatusDisplay status;

    public override void OnDeserialization()
    {
        status.RefreshUI();
    }
}
