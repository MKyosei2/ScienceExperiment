using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(Collider))]
public class ZonePickupController : MonoBehaviour
{
    [Tooltip("このオブジェクトを拾えるゾーン（複数可）。null/空で常に不可。")]
    [SerializeField] private Collider[] allowedZones;
    [SerializeField] private bool pickupableOutsideZones = false;

    private Component _pickupLike;     // VRC_Pickupインスタンス（あれば）
    private PropertyInfo _piPickupable;
    private int _insideCount = 0;

    private void Awake()
    {
        _pickupLike = GetComponentByTypeName("VRC_Pickup") ?? GetComponentInChildrenByTypeName("VRC_Pickup");
        if (_pickupLike != null)
        {
            _piPickupable = _pickupLike.GetType().GetProperty("pickupable",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_piPickupable == null)
                Debug.LogWarning("[ZonePickupController] Found VRC_Pickup but no 'pickupable' property.");
        }
        Apply();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsAllowedZone(other)) { _insideCount++; Apply(); }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsAllowedZone(other)) { _insideCount = Mathf.Max(0, _insideCount - 1); Apply(); }
    }

    private bool IsAllowedZone(Collider c)
    {
        if (allowedZones == null) return false;
        foreach (var z in allowedZones) if (z == c) return true;
        return false;
    }

    private void Apply()
    {
        bool allow = pickupableOutsideZones || _insideCount > 0;
        if (_pickupLike != null && _piPickupable != null)
            _piPickupable.SetValue(_pickupLike, allow);
    }

    private Component GetComponentByTypeName(string typeName)
    {
        foreach (var c in GetComponents<Component>())
            if (c && c.GetType().Name == typeName) return c;
        return null;
    }

    private Component GetComponentInChildrenByTypeName(string typeName)
    {
        foreach (var c in GetComponentsInChildren<Component>(true))
            if (c && c.GetType().Name == typeName) return c;
        return null;
    }
}
