using UdonSharp;
using UnityEngine;

public class SpawnSelectorButton : UdonSharpBehaviour
{
    [Header("カテゴリ設定 (Inspector でプルダウン)")]
    public SelectionCategory category = SelectionCategory.Element;

    [Header("参照")]
    public ChemElementSpawner elementSpawner;
    public ChemEnvironmentManager environmentManager;
    public ModeRouter modeRouter;

    public override void Interact()
    {
        Press();
    }

    public void Press()
    {
        switch (category)
        {
            case SelectionCategory.Element:
                if (elementSpawner != null) elementSpawner.Spawn();
                break;

            case SelectionCategory.Equipment:
                if (environmentManager != null) environmentManager.SetEquipment(0); // 今は単一
                break;

            case SelectionCategory.TemperatureUp:
                if (environmentManager != null) environmentManager.AdjustTemperature(+1f);
                break;

            case SelectionCategory.TemperatureDown:
                if (environmentManager != null) environmentManager.AdjustTemperature(-1f);
                break;

            case SelectionCategory.HumidityUp:
                Debug.Log("[SpawnSelectorButton] 湿度＋ 未実装");
                break;

            case SelectionCategory.HumidityDown:
                Debug.Log("[SpawnSelectorButton] 湿度－ 未実装");
                break;

            case SelectionCategory.PressureUp:
                if (environmentManager != null) environmentManager.AdjustPressure(+1f);
                break;

            case SelectionCategory.PressureDown:
                if (environmentManager != null) environmentManager.AdjustPressure(-1f);
                break;

            case SelectionCategory.StartExperiment:
                if (modeRouter != null && !modeRouter.IsVR())
                {
                    if (elementSpawner != null) elementSpawner.StartExperiment();
                }
                break;

            case SelectionCategory.ModeToggle:
                if (modeRouter != null) modeRouter.Toggle();
                break;

            case SelectionCategory.Reset:
                if (elementSpawner != null) elementSpawner.ResetExperiment();
                break;
        }
    }
}
