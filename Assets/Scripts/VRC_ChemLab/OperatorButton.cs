using UdonSharp;
using UnityEngine;

/// <summary>
/// OperatorButton
/// mode="claim" なら操作権を取る（最初の操作で自動取得でもOK）
/// mode="release"なら操作権を手放す
/// </summary>
public class OperatorButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public string mode = "claim"; // "claim" or "release"

    public override void Interact()
    {
        if (spawner == null) return;

        if (mode == "release")
            spawner.SendCustomEvent("_ReleaseOperator");
        else
            spawner.SendCustomEvent("_StartExperiment"); // Startで自動Claimされる設計
    }
}
