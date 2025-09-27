using UdonSharp;
using UnityEngine;

// 既存コードが参照するグローバル enum
public enum SelectionCategory
{
    Element,
    Compound,
    Equipment,
    Tool,
    Condition,
    Other
}

// UdonSharpProgramAsset が割り当てられても問題ないように
[AddComponentMenu("VRC Lab/SelectorCategory")]
public class SelectorCategory : UdonSharpBehaviour
{
    [Header("このオブジェクトのカテゴリ")]
    public SelectionCategory category = SelectionCategory.Element;
}
