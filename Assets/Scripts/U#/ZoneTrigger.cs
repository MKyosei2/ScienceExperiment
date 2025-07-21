using UdonSharp;
using UnityEngine;

public class ZoneTrigger : UdonSharpBehaviour
{
    [Tooltip("ゾーンの種類（Element / Tool / Condition など）")]
    public string zoneType = "Element";

    public void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        GameObject obj = other.gameObject;
        Debug.Log($"{zoneType}Zone: {obj.name} entered.");

        // 必要に応じて名前などで条件判定する（例：要素名に "Element_" が含まれていればOK）
        if (obj.name.StartsWith("Element_"))
        {
            Debug.Log("Valid element entered.");
            // Manager に通知するなどの処理をここに追加
        }
    }
}
