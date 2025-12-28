using UdonSharp;
using UnityEngine;

/// <summary>
/// ConditionAdjuster
/// ・ボタン（キューブ）から環境値を調整する。
/// ・spawnerが設定されている場合は、spawner経由で操作権チェック＆同期を行う。
/// </summary>
public class ConditionAdjuster : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public ChemElementSpawner spawner; // optional
    public string command; // Modify に渡す

    public override void Interact()
    {
        if (spawner != null)
        {
            spawner.ModifyEnvironment(command);
            return;
        }

        if (env != null) env.Modify(command);
    }
}
