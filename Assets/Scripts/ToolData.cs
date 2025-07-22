using UnityEngine;

[CreateAssetMenu(fileName = "ToolData", menuName = "ChemLab/Tool")]
public class ToolData : ScriptableObject
{
    public string toolID;
    public string toolName;
    public GameObject displayPrefab;
}