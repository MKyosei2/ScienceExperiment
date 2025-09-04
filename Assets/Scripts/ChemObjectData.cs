using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/ChemObjectData", fileName = "ChemObjectData")]
public class ChemObjectData : ScriptableObject
{
    public enum ChemType { Element, Tool, Condition }

    [Header("Common")]
    public string id;
    public string displayName;
    public ChemType type;
    public GameObject displayPrefab;

    [Header("Optional: Element-only")]
    public ElementExtra element;

    [System.Serializable]
    public struct ElementExtra
    {
        public bool enabled;
        public int atomicNumber;
        public int group;
        public int period;
        public string symbol;
    }
}
