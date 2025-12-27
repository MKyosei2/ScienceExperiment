using UdonSharp;
using UnityEngine;

public class EnvUISyncBridge : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public ChemStatusDisplay status;

    public override void OnDeserialization()
    {
        if (status != null) status.RefreshUI();
    }


// Orchestrator/ボタンから呼ぶ用（明示更新）
public void _RefreshAllDisplays()
{
    if (status != null) status.RefreshUI();
}

}
