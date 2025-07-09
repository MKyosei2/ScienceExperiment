using UnityEngine;

[CreateAssetMenu(fileName = "ConditionData", menuName = "ChemLab/Condition")]
public class ConditionData : ScriptableObject
{
    [Header("識別情報")]
    public string conditionID;

    [Header("視覚表示")]
    public string displayName;
    public GameObject displayPrefab;
    public Color displayColor = Color.white;
}
