using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// 実験全体の進行を管理する
/// </summary>
public class ExperimentOrchestrator : UdonSharpBehaviour
{
    [Header("▼ 各コンポーネント参照")]
    public ChemElementSpawner elementSpawner;
    public AIRequestSender aiRequestSender;

    [Header("▼ 実験状態フラグ")]
    public bool isPCMode = true;

    /// <summary>
    /// 実験開始ボタンが押されたとき
    /// </summary>
    public void StartExperiment()
    {
        if (elementSpawner == null) return;

        // 見た目や液体の挙動を開始
        elementSpawner.StartExperiment();

        // 分子構造をJSONにまとめてAIに送信
        if (aiRequestSender != null)
        {
            string moleculeJson = elementSpawner.SendMoleculeJson();
            aiRequestSender.Run(moleculeJson, elementSpawner);
        }
    }

    /// <summary>
    /// リセットボタンが押されたとき
    /// </summary>
    public void ResetExperiment()
    {
        if (elementSpawner != null)
        {
            elementSpawner.ResetExperiment();
        }
    }

    /// <summary>
    /// PCモードとVRモードの切り替え
    /// </summary>
    public void ToggleMode()
    {
        isPCMode = !isPCMode;
        Debug.Log("[ExperimentOrchestrator] Mode switched: " + (isPCMode ? "PC" : "VR"));
    }
}
