using UdonSharp;
using UnityEngine;

public class MixingBehaviour : FlaskBehaviour
{
    public Transform liquidSurface;

    public override void OnElementApplied(int elementId)
    {
        base.OnElementApplied(elementId);
    }

    public override void Update()
    {
        if (!isActive) return;
        if (liquidSurface != null)
        {
            float angle = Mathf.Sin(Time.time * 2f) * 10f;
            liquidSurface.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
