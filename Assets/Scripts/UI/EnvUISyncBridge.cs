using UdonSharp;
using UnityEngine;

/// <summary>
/// EnvUISyncBridge
/// UI表示更新をまとめるだけのブリッジ。
/// （同期の真実はspawnerが持つ。UI更新はローカル。）
/// </summary>
public class EnvUISyncBridge : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public ChemStatusDisplay status;

    public override void OnDeserialization()
    {
        _RefreshAllDisplays();
    }

    public void _RefreshAllDisplays()
    {
        if (status != null) status.RefreshUI();
    }
}
