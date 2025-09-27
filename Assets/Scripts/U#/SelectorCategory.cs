using UdonSharp;
using UnityEngine;

public enum SelectionCategory
{
    Element,
    Compound,
    Equipment,
    Tool,
    Condition,
    TemperatureUp,
    TemperatureDown,
    HumidityUp,
    HumidityDown,
    PressureUp,
    PressureDown,
    StartExperiment,
    ModeToggle,
    Reset
}

[AddComponentMenu("VRC Lab/SelectorCategory")]
public class SelectorCategory : UdonSharpBehaviour
{
    [Header("このオブジェクトのカテゴリ")]
    public SelectionCategory category = SelectionCategory.Element;
}
