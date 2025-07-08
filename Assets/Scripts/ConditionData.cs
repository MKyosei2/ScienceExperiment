using UnityEngine;

[CreateAssetMenu(fileName = "ConditionData", menuName = "ChemLab/Condition")]
public class ConditionData : ScriptableObject
{
    public string conditionID;
    public string displayName;
    public string description;
    public bool useGravity = true;
    public float temperatureCelsius = 25f;
    public float pressureAtm = 1f;
    public bool lightEnabled = true;
}