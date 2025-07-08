using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "ChemLab/Element")]
public class ElementData : ScriptableObject
{
    public string symbol;
    public string elementName;
    public int atomicNumber;
    public Color displayColor;
    public GameObject displayPrefab;
}