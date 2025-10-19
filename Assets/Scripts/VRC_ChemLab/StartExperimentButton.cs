using UdonSharp;
using UnityEngine;
using VRC.Udon;

[AddComponentMenu("VRC Lab/StartExperimentButton")]
public class StartExperimentButton : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;

    public void _OnClick()
    {
        if (spawner != null)
        {
            spawner.SendCustomEvent("_StartExperiment");
            Debug.Log("[StartExperimentButton] 実験開始ボタンが押されました");
        }
        else
        {
            Debug.LogWarning("[StartExperimentButton] ChemElementSpawner が設定されていません");
        }
    }
}
