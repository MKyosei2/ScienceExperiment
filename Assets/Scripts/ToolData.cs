using UnityEngine;

[CreateAssetMenu(fileName = "ToolData", menuName = "ChemLab/Tool")]
public class ToolData : ScriptableObject
{
    [Header("Šî–{î•ñ")]
    public string toolName;
    public string toolID;
    public string description;
    public Sprite icon;

    [Header("“®ì")]
    public bool isReusable;
    public bool requiresPower;

    [Header("‹Šo")]
    public Color toolColor = Color.white;
    public GameObject toolPrefab;

    public string Summary()
    {
        return $"{toolName} ({toolID})\n{description}";
    }
}