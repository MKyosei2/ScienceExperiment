using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "ChemLab/Element")]
public class ElementData : ScriptableObject
{
    public string elementID;
    public string elementName;
    public int atomicNumber;
    public GameObject displayPrefab;
    public int group;              // ‘°”Ô†
    public int period;             // üŠú”Ô†
}