using UdonSharp;
using UnityEngine;

public class ZoneTrigger : UdonSharpBehaviour
{
    public string acceptedTag = "ExperimentObject";
    public string zoneType = "Element"; // "Tool", "Condition" など

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.tag == acceptedTag)
        {
            Debug.Log($"{zoneType}Zone: {other.name} entered.");
            // 必要な処理をここに追加
        }
    }
}
