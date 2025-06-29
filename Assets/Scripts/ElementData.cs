using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "ChemLab/Element")]
public class ElementData : ScriptableObject
{
    [Header(@"基本\情報")]
    public string elementName;
    public string symbol;
    public int atomicNumber;
    public int group;
    public int period;
    public ElementCategory category;

    [Header(@"構造・状態情報")]
    public string electronConfiguration;
    public string phase;
    public float atomicMass;
    public float atomicRadius;
    public float density;
    public float meltingPoint;
    public float boilingPoint;

    [Header(@"化学的特性")]
    public float electronegativity;
    public int valenceElectrons;
    public float ionizationEnergy;
    public float electronAffinity;
    public bool isRadioactive;

    [Header(@"用途・発見")]
    public string commonUses;
    public string discoveredBy;
    public int discoveryYear;

    [Header(@"生体・安全性")]
    public bool isEssentialToLife;
    public string toxicityInfo;

    [Header(@"見た目")]
    public Color displayColor = Color.white;
    public GameObject displayPrefab;

    public enum ElementCategory
    {
        Nonmetal,
        Metal,
        Metalloid,
        NobleGas,
        Halogen,
        AlkaliMetal,
        AlkalineEarth,
        TransitionMetal,
        PostTransitionMetal,
        Lanthanide,
        Actinide,
        Unknown
    }

    public string GetSummary()
    {
        return $"[{symbol}] {elementName} (原子番号 {atomicNumber})\n分類: {category}, 状態: {phase}, 質量: {atomicMass} u\n用途: {commonUses}";
    }

    public bool IsMetallic()
    {
        return category == ElementCategory.Metal ||
               category == ElementCategory.AlkaliMetal ||
               category == ElementCategory.AlkalineEarth ||
               category == ElementCategory.TransitionMetal ||
               category == ElementCategory.Lanthanide ||
               category == ElementCategory.Actinide;
    }

    public bool IsNobleGas()
    {
        return category == ElementCategory.NobleGas;
    }

    [Header("キャッシュ")]
    public string cachedSymbol;

#if UNITY_EDITOR
void OnValidate()
{
    cachedSymbol = symbol;
}
#endif
}
