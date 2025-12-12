using UdonSharp;
using UnityEngine;

public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager env;
    public string command;   // "TEMP+", "TEMP-", "HUMID+", "PRESS-" など

    public override void Interact()
    {
        if (env == null)
        {
            Debug.LogError("[ValueAdjustButton] EnvironmentManager が設定されていません");
            return;
        }

        if (string.IsNullOrEmpty(command))
        {
            Debug.LogError("[ValueAdjustButton] command が空です");
            return;
        }

        Debug.Log($"[ValueAdjustButton] Execute command: {command}");

        env.Modify(command);     // ← SetProgramVariable を使わず確実に呼べる
    }
}
