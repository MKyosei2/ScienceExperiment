using UdonSharp;
using UnityEngine;

// グローバル enum (既存コードが参照するもの)
public enum SelectionCategory
{
    Element,
    Compound,
    Equipment,
    Tool,
    Condition,
    Other
}

// UdonSharpBehaviour を持つダミークラス (Unity の U# Program Asset が参照する用)
[AddComponentMenu("VRC Lab/SelectorCategory")]
public class SelectorCategory : UdonSharpBehaviour
{
    [Header("このオブジェクトのカテゴリ")]
    public SelectionCategory category = SelectionCategory.Element;
}
